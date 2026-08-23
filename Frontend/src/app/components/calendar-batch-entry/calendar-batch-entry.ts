import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { catchError, finalize, forkJoin, from, map, of, take } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  DayEntryType,
  LeaveEntry,
  LeaveBatchCreateResult,
  LeaveType,
  WorkLocationType,
  WorkLocationEntry,
  WorkLocationBatchCreateResult,
} from '../../models/api.models';
import { LeaveService } from '../../services/leave';
import { PublicHolidayService } from '../../services/public-holiday';
import { WorkLocationService } from '../../services/work-location';
import { parseApiError } from '../../utils/api-problem';
import {
  CalendarChangeRow,
  CalendarDayCategory,
  CalendarOperationResult,
  isCalendarRowChanged,
  planCalendarChanges,
  reconcileCalendarChanges,
} from '../../utils/calendar-change-set';
import { CalendarBatchDayCellComponent } from './calendar-batch-day-cell.component';

type DayCategory = CalendarDayCategory;

interface HolidayDayInfo {
  name: string;
  canBeWorkedOn: boolean;
}

interface CalendarDayRowVm extends CalendarChangeRow {
  dayLabel: string;
  isToday: boolean;
  isHoliday: boolean;
  holidayName: string;
  canBeWorkedOnHoliday: boolean;
  resultState: 'applied' | 'failed' | 'skipped' | null;
  resultMessage: string;
}

interface CalendarWeekVm {
  weekLabel: string;
  weekStartIso: string;
  days: Array<CalendarDayRowVm | null>;
}

@Component({
  selector: 'app-calendar-batch-entry',
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    CalendarBatchDayCellComponent,
  ],
  templateUrl: './calendar-batch-entry.html',
  styleUrl: './calendar-batch-entry.scss',
})
export class CalendarBatchEntry implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly publicHolidayService = inject(PublicHolidayService);
  private readonly workLocationService = inject(WorkLocationService);
  private readonly leaveService = inject(LeaveService);

  readonly entryTypeOptions = [
    { value: DayEntryType.FullDay, label: 'Full Day' },
    { value: DayEntryType.HalfDay, label: 'Half Day' },
    { value: DayEntryType.SpecificHours, label: 'Specific Hours' },
  ];

  readonly categoryOptions: Array<{ value: DayCategory; label: string }> = [
    { value: 'none', label: 'None' },
    { value: 'wfh', label: 'WFH' },
    { value: 'office', label: 'Office' },
    { value: 'annual', label: 'Annual' },
    { value: 'sick', label: 'Sick' },
  ];

  monthAnchor = this.startOfMonth(new Date());
  rows: CalendarDayRowVm[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.loadMonth();
  }

  get monthLabel(): string {
    return new Intl.DateTimeFormat('en-AU', { month: 'long', year: 'numeric' }).format(this.monthAnchor);
  }

  get pendingCount(): number {
    return this.rows.filter((row) => this.isRowChanged(row)).length;
  }

  get hasValidationErrors(): boolean {
    return this.rows.some((row) => this.rowHasHoursError(row));
  }

  get isMonthNavigationDisabled(): boolean {
    return this.pendingCount > 0 || this.isLoading || this.isSubmitting;
  }

  get weeks(): CalendarWeekVm[] {
    if (this.rows.length === 0) {
      return [];
    }

    const weekMap = new Map<string, CalendarWeekVm>();

    for (const row of this.rows) {
      const rowDate = this.parseDateIso(row.dateIso);
      const weekStart = this.getWeekStart(rowDate);
      const weekStartIso = this.toDateIso(weekStart);

      let week = weekMap.get(weekStartIso);
      if (!week) {
        const weekEnd = new Date(weekStart.getFullYear(), weekStart.getMonth(), weekStart.getDate() + 4);
        week = {
          weekStartIso,
          weekLabel: `${this.formatShortDate(weekStart)} - ${this.formatShortDate(weekEnd)}`,
          days: [null, null, null, null, null],
        };
        weekMap.set(weekStartIso, week);
      }

      const dayIndex = rowDate.getDay() - 1;
      if (dayIndex >= 0 && dayIndex <= 4) {
        week.days[dayIndex] = row;
      }
    }

    return Array.from(weekMap.values()).sort((a, b) => a.weekStartIso.localeCompare(b.weekStartIso));
  }

  goToPreviousMonth(): void {
    this.monthAnchor = new Date(this.monthAnchor.getFullYear(), this.monthAnchor.getMonth() - 1, 1);
    this.loadMonth();
  }

  goToNextMonth(): void {
    this.monthAnchor = new Date(this.monthAnchor.getFullYear(), this.monthAnchor.getMonth() + 1, 1);
    this.loadMonth();
  }

  goToCurrentMonth(): void {
    this.monthAnchor = this.startOfMonth(new Date());
    this.loadMonth();
  }

  onCategoryChange(row: CalendarDayRowVm, category: DayCategory): void {
    if (this.isHolidayLocked(row)) {
      row.category = 'none';
      return;
    }

    row.category = category;
    if (category === 'none') {
      row.entryType = DayEntryType.FullDay;
      row.specificHours = null;
      row.leaveType = row.originalLeaveType;
    } else if (this.isLeaveCategory(category)) {
      row.leaveType = this.toLeaveType(category);
    }

    this.clearRowResult(row);
  }

  onEntryTypeChange(row: CalendarDayRowVm, entryType: DayEntryType): void {
    row.entryType = entryType;

    if (entryType !== DayEntryType.SpecificHours) {
      row.specificHours = null;
    }

    this.clearRowResult(row);
  }

  setWeekToWfh(week: CalendarWeekVm): void {
    let changedCount = 0;

    for (const row of week.days) {
      if (!row || row.isHoliday) {
        continue;
      }

      if (row.category !== 'wfh') {
        changedCount += 1;
      }

      this.onCategoryChange(row, 'wfh');
    }

    this.infoMessage = changedCount === 0
      ? 'No weekdays in this week needed changing.'
      : `${this.dayCountLabel(changedCount)} set to WFH for this week. Public holidays were skipped.`;
    this.errorMessage = '';
  }

  isSpecificHoursRow(row: CalendarDayRowVm): boolean {
    return row.category !== 'none' && row.entryType === DayEntryType.SpecificHours;
  }

  rowHasHoursError(row: CalendarDayRowVm): boolean {
    if (!this.isSpecificHoursRow(row)) {
      return false;
    }

    const hours = row.specificHours;
    return !hours || hours <= 0 || hours > 24;
  }

  queueBatchAdd(): void {
    if (this.isSubmitting) {
      return;
    }

    if (this.pendingCount === 0) {
      this.errorMessage = '';
      this.infoMessage = 'Nothing was changed.';
      return;
    }

    if (this.hasValidationErrors) {
      this.errorMessage = 'Fix the invalid specific hours before using Batch Add.';
      this.infoMessage = '';
      return;
    }

    const plan = planCalendarChanges(this.rows);
    const rowsByDate = new Map(this.rows.map((row) => [row.dateIso, row]));
    for (const dateIso of plan.changedDateIsos) {
      const row = rowsByDate.get(dateIso);
      if (row)
        this.clearRowResult(row);
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    forkJoin({
      wfhCreate: plan.workCreates.length === 0
        ? of({ result: this.emptyWfhBatchResult(), error: undefined as string | undefined })
        : this.workLocationService.createBatch({ items: plan.workCreates.map((operation) => operation.payload) }).pipe(
            map((result) => ({ result, error: undefined as string | undefined })),
            catchError((error) =>
              from(parseApiError(error, 'Work-location batch request failed.')).pipe(
                map((parsedError) => ({
                  result: this.failedWfhBatchResult(plan.workCreates.length, parsedError.message),
                  error: parsedError.message,
                })),
              ),
            ),
          ),
      leaveCreate: plan.leaveCreates.length === 0
        ? of({ result: this.emptyLeaveBatchResult(), error: undefined as string | undefined })
        : this.leaveService.createBatch({ items: plan.leaveCreates.map((operation) => operation.payload) }).pipe(
            map((result) => ({ result, error: undefined as string | undefined })),
            catchError((error) =>
              from(parseApiError(error, 'Leave batch request failed.')).pipe(
                map((parsedError) => ({
                  result: this.failedLeaveBatchResult(plan.leaveCreates.length, parsedError.message),
                  error: parsedError.message,
                })),
              ),
            ),
          ),
      wfhDelete: plan.workDeletes.length === 0
        ? of([] as CalendarOperationResult[])
        : forkJoin(
            plan.workDeletes.map((op) =>
              this.workLocationService.softDelete(op.id).pipe(
                map(() => ({ dateIso: op.dateIso, ok: true, message: '' })),
                catchError(() => of({ dateIso: op.dateIso, ok: false, message: 'Failed to remove existing work-location entry.' })),
              ),
            ),
          ),
      leaveDelete: plan.leaveDeletes.length === 0
        ? of([] as CalendarOperationResult[])
        : forkJoin(
            plan.leaveDeletes.map((op) =>
              this.leaveService.softDelete(op.id).pipe(
                map(() => ({ dateIso: op.dateIso, ok: true, message: '' })),
                catchError(() => of({ dateIso: op.dateIso, ok: false, message: 'Failed to remove existing leave entry.' })),
              ),
            ),
          ),
      wfhUpdate: plan.workUpdates.length === 0
        ? of([] as CalendarOperationResult[])
        : forkJoin(
            plan.workUpdates.map((op) =>
              this.workLocationService.update(op.id, op.payload!).pipe(
                map(() => ({ dateIso: op.dateIso, ok: true, message: '' })),
                catchError(() => of({ dateIso: op.dateIso, ok: false, message: 'Failed to update existing work-location entry.' })),
              ),
            ),
          ),
      leaveUpdate: plan.leaveUpdates.length === 0
        ? of([] as CalendarOperationResult[])
        : forkJoin(
            plan.leaveUpdates.map((op) =>
              this.leaveService.update(op.id, op.payload!).pipe(
                map(() => ({ dateIso: op.dateIso, ok: true, message: '' })),
                catchError(() => of({ dateIso: op.dateIso, ok: false, message: 'Failed to update existing leave entry.' })),
              ),
            ),
          ),
    })
      .pipe(
        map(({ wfhCreate, leaveCreate, wfhDelete, leaveDelete, wfhUpdate, leaveUpdate }) => {
          const reconciliation = reconcileCalendarChanges(this.rows, plan, {
            workCreates: wfhCreate.result,
            leaveCreates: leaveCreate.result,
            workDeletes: wfhDelete,
            leaveDeletes: leaveDelete,
            workUpdates: wfhUpdate,
            leaveUpdates: leaveUpdate,
            workCreateError: wfhCreate.error,
            leaveCreateError: leaveCreate.error,
          });

          for (const result of reconciliation.rows) {
            const row = rowsByDate.get(result.dateIso);
            if (!row)
              continue;

            if (result.workLocationId !== undefined)
              row.workLocationId = result.workLocationId;
            if (result.leaveId !== undefined)
              row.leaveId = result.leaveId;

            row.resultState = result.state;
            row.resultMessage = result.message;
            if (result.syncOriginal)
              this.syncOriginalState(row);
          }

          return reconciliation;
        }),
        finalize(() => {
          this.isSubmitting = false;
        }),
      )
      .subscribe({
        next: ({ skipped, failed, added, updated }) => {
          this.infoMessage = `Batch apply complete: Added ${this.dayCountLabel(added)}, updated ${this.dayCountLabel(updated)}, skipped ${skipped}, failed ${failed}.`;
          this.errorMessage = failed > 0 ? 'Some rows failed. Fix the highlighted rows and submit again.' : '';

          if (failed === 0) {
            this.loadMonth(true);
          }
        },
        error: () => {
          this.errorMessage = 'Unable to submit batch changes.';
          this.infoMessage = '';
        },
      });
  }

  private emptyWfhBatchResult(): WorkLocationBatchCreateResult {
    return {
      totalRequested: 0,
      createdCount: 0,
      skippedCount: 0,
      failedCount: 0,
      results: [],
    };
  }

  private failedWfhBatchResult(totalRequested: number, message: string): WorkLocationBatchCreateResult {
    void message;
    return {
      totalRequested,
      createdCount: 0,
      skippedCount: 0,
      failedCount: totalRequested,
      results: [],
    };
  }

  private emptyLeaveBatchResult(): LeaveBatchCreateResult {
    return {
      totalRequested: 0,
      createdCount: 0,
      skippedCount: 0,
      failedCount: 0,
      results: [],
    };
  }

  private failedLeaveBatchResult(totalRequested: number, message: string): LeaveBatchCreateResult {
    void message;
    return {
      totalRequested,
      createdCount: 0,
      skippedCount: 0,
      failedCount: totalRequested,
      results: [],
    };
  }

  private loadMonth(preserveMessages = false): void {
    this.isLoading = true;
    if (!preserveMessages) {
      this.errorMessage = '';
      this.infoMessage = '';
    }

    const fromDate = this.toDateIso(this.monthAnchor);
    const toDate = this.toDateIso(this.endOfMonth(this.monthAnchor));

    forkJoin({
      holidays: this.publicHolidayService.getAll({ fromDate, toDate }).pipe(
        catchError(() => of([])),
      ),
      leave: this.leaveService.getAll({ fromDate, toDate }),
      wfh: this.workLocationService.getAll({ fromDate, toDate }),
    })
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: ({ holidays, leave, wfh }) => {
          const holidayMap = new Map<string, HolidayDayInfo>();
          for (const holiday of holidays) {
            const dateKey = this.toDateKey(holiday.holidayDate);
            const existing = holidayMap.get(dateKey);

            if (!existing) {
              holidayMap.set(dateKey, {
                name: holiday.name,
                canBeWorkedOn: holiday.canBeWorkedOn,
              });
              continue;
            }

            holidayMap.set(dateKey, {
              name: existing.name,
              canBeWorkedOn: existing.canBeWorkedOn && holiday.canBeWorkedOn,
            });
          }

          const leaveByDate = this.toLeaveMap(leave);
          const wfhByDate = this.toWfhMap(wfh);

          this.rows = this.buildWeekdayRows(this.monthAnchor, holidayMap, leaveByDate, wfhByDate);
        },
        error: () => {
          this.rows = [];
          this.errorMessage = 'Unable to load calendar entries.';
        },
      });
  }

  private buildWeekdayRows(
    anchor: Date,
    holidayMap: Map<string, HolidayDayInfo>,
    leaveByDate: Map<string, LeaveEntry>,
    wfhByDate: Map<string, WorkLocationEntry>,
  ): CalendarDayRowVm[] {
    const rows: CalendarDayRowVm[] = [];
    const year = anchor.getFullYear();
    const month = anchor.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const today = this.toDateIso(new Date());

    for (let day = 1; day <= daysInMonth; day += 1) {
      const date = new Date(year, month, day);
      const weekDay = date.getDay();

      if (weekDay === 0 || weekDay === 6) {
        continue;
      }

      const dateIso = this.toDateIso(date);
      const holiday = holidayMap.get(dateIso);
      const holidayName = holiday?.name ?? '';
      const canBeWorkedOnHoliday = holiday?.canBeWorkedOn ?? false;
      const leaveEntry = leaveByDate.get(dateIso) ?? null;
      const wfhEntry = wfhByDate.get(dateIso) ?? null;

      const originalCategory: DayCategory = leaveEntry
        ? this.toCategoryFromLeaveType(leaveEntry.leaveType)
        : wfhEntry
          ? this.toCategoryFromWorkLocation(wfhEntry.workLocation)
          : 'none';
      const originalEntryType = leaveEntry?.entryType ?? wfhEntry?.entryType ?? DayEntryType.FullDay;
      const originalSpecificHours =
        originalEntryType === DayEntryType.SpecificHours
          ? (leaveEntry?.hoursWorked ?? wfhEntry?.hoursWorked ?? null)
          : null;
      const originalLeaveType = leaveEntry?.leaveType ?? LeaveType.Annual;

      rows.push({
        dateIso,
        dayLabel: new Intl.DateTimeFormat('en-AU', { weekday: 'short', day: '2-digit', month: 'short' }).format(date),
        isToday: dateIso === today,
        category: originalCategory,
        originalCategory,
        entryType: originalEntryType,
        originalEntryType,
        specificHours: originalSpecificHours,
        originalSpecificHours,
        leaveType: originalLeaveType,
        originalLeaveType,
        leaveId: leaveEntry?.id ?? null,
        workLocationId: wfhEntry?.id ?? null,
        isHoliday: Boolean(holidayName),
        holidayName,
        canBeWorkedOnHoliday,
        resultState: null,
        resultMessage: '',
      });
    }

    return rows;
  }

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }

  private endOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth() + 1, 0);
  }

  private toDateIso(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private parseDateIso(dateIso: string): Date {
    const [year, month, day] = dateIso.split('-').map(Number);
    return new Date(year, (month ?? 1) - 1, day ?? 1);
  }

  private getWeekStart(date: Date): Date {
    const day = date.getDay();
    const offset = day === 0 ? -6 : 1 - day;
    return new Date(date.getFullYear(), date.getMonth(), date.getDate() + offset);
  }

  private formatShortDate(date: Date): string {
    return new Intl.DateTimeFormat('en-AU', { day: '2-digit', month: 'short' }).format(date);
  }

  private toLeaveMap(entries: LeaveEntry[]): Map<string, LeaveEntry> {
    const result = new Map<string, LeaveEntry>();
    for (const entry of entries) {
      result.set(this.toDateKey(entry.leaveDate), entry);
    }

    return result;
  }

  isHolidayLocked(row: CalendarDayRowVm): boolean {
    return row.isHoliday && !row.canBeWorkedOnHoliday;
  }

  private toWfhMap(entries: WorkLocationEntry[]): Map<string, WorkLocationEntry> {
    const result = new Map<string, WorkLocationEntry>();
    for (const entry of entries) {
      result.set(this.toDateKey(entry.workDate), entry);
    }

    return result;
  }

  isRowChanged(row: CalendarDayRowVm): boolean {
    return isCalendarRowChanged(row);
  }

  private isLeaveCategory(category: DayCategory): boolean {
    return category === 'annual' || category === 'sick';
  }

  private toCategoryFromWorkLocation(workLocation: WorkLocationType): DayCategory {
    return workLocation === WorkLocationType.Office ? 'office' : 'wfh';
  }

  private toCategoryFromLeaveType(leaveType: LeaveType): DayCategory {
    return leaveType === LeaveType.Sick ? 'sick' : 'annual';
  }

  private toLeaveType(category: DayCategory): LeaveType {
    return category === 'sick' ? LeaveType.Sick : LeaveType.Annual;
  }

  private dayCountLabel(count: number): string {
    return `${count} ${count === 1 ? 'day' : 'days'}`;
  }

  private clearRowResult(row: CalendarDayRowVm): void {
    row.resultState = null;
    row.resultMessage = '';
  }

  private syncOriginalState(row: CalendarDayRowVm): void {
    row.originalCategory = row.category;
    row.originalEntryType = row.entryType;
    row.originalSpecificHours = row.entryType === DayEntryType.SpecificHours ? row.specificHours : null;
    row.originalLeaveType = row.leaveType;

    if (row.category === 'none') {
      row.leaveId = null;
      row.workLocationId = null;
    }
  }

  private toDateKey(value: string): string {
    const raw = (value ?? '').trim();
    if (raw.length === 0) {
      return raw;
    }

    const tIndex = raw.indexOf('T');
    if (tIndex > 0) {
      return raw.slice(0, tIndex);
    }

    if (raw.length >= 10) {
      return raw.slice(0, 10);
    }

    return raw;
  }
}
