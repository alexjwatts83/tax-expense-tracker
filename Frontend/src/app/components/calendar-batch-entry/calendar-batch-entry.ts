import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, finalize, forkJoin, map, of, take } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import {
  CreateLeaveRequest,
  CreateWorkLocationRequest,
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

type DayCategory = 'none' | 'wfh' | 'office' | 'annual' | 'sick';

interface CalendarDayRowVm {
  dateIso: string;
  dayLabel: string;
  isToday: boolean;
  category: DayCategory;
  originalCategory: DayCategory;
  entryType: DayEntryType;
  originalEntryType: DayEntryType;
  specificHours: number | null;
  originalSpecificHours: number | null;
  leaveType: LeaveType;
  originalLeaveType: LeaveType;
  leaveId: string | null;
  workLocationId: string | null;
  isHoliday: boolean;
  holidayName: string;
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
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatSelectModule,
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

  clearAll(): void {
    for (const row of this.rows) {
      if (row.isHoliday) {
        continue;
      }

      row.category = row.originalCategory;
      row.entryType = row.originalEntryType;
      row.specificHours = row.originalSpecificHours;
      row.leaveType = row.originalLeaveType;
    }

    this.infoMessage = 'All pending changes were reverted.';
    this.errorMessage = '';
  }

  setAllCategory(category: Exclude<DayCategory, 'none'>): void {
    for (const row of this.rows) {
      if (row.isHoliday) {
        continue;
      }

      row.category = category;
      row.leaveType = this.toLeaveType(category);
      if (row.entryType !== DayEntryType.SpecificHours) {
        row.specificHours = null;
      }
    }

    this.infoMessage = `All weekday rows were set to ${this.categoryLabel(category)}.`;
    this.errorMessage = '';
  }

  onCategoryChange(row: CalendarDayRowVm, category: DayCategory): void {
    if (row.isHoliday) {
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

    const changedRows = this.rows.filter((row) => this.isRowChanged(row));

    const workCreateOps = changedRows
      .filter((row) => this.requiresWorkCreate(row))
      .map((row) => ({
        row,
        payload: {
          workDate: row.dateIso,
          workLocation: this.toWorkLocationType(row.category),
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateWorkLocationRequest,
      }));

    const leaveCreateOps = changedRows
      .filter((row) => this.requiresLeaveCreate(row))
      .map((row) => ({
        row,
        payload: {
          leaveDate: row.dateIso,
          leaveType: row.leaveType,
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateLeaveRequest,
      }));

    const workDeleteOps = changedRows
      .filter((row) => this.requiresWorkDelete(row) && row.workLocationId)
      .map((row) => ({
        row,
        id: row.workLocationId as string,
      }));

    const leaveDeleteOps = changedRows
      .filter((row) => this.requiresLeaveDelete(row) && row.leaveId)
      .map((row) => ({
        row,
        id: row.leaveId as string,
      }));

    const workUpdateOps = changedRows
      .filter((row) => this.requiresWorkUpdate(row) && row.workLocationId)
      .map((row) => ({
        row,
        id: row.workLocationId as string,
        payload: {
          workDate: row.dateIso,
          workLocation: this.toWorkLocationType(row.category),
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateWorkLocationRequest,
      }));

    const leaveUpdateOps = changedRows
      .filter((row) => this.requiresLeaveUpdate(row) && row.leaveId)
      .map((row) => ({
        row,
        id: row.leaveId as string,
        payload: {
          leaveDate: row.dateIso,
          leaveType: row.leaveType,
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateLeaveRequest,
      }));

    for (const row of changedRows) {
      this.clearRowResult(row);
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    forkJoin({
      wfhCreate: workCreateOps.length === 0
        ? of<WorkLocationBatchCreateResult>(this.emptyWfhBatchResult())
        : this.workLocationService.createBatch({ items: workCreateOps.map((x) => x.payload) }).pipe(
            catchError((err) =>
              of<WorkLocationBatchCreateResult>(
                this.failedWfhBatchResult(workCreateOps.length, err?.error?.detail ?? 'Work-location batch request failed.'),
              ),
            ),
          ),
      leaveCreate: leaveCreateOps.length === 0
        ? of<LeaveBatchCreateResult>(this.emptyLeaveBatchResult())
        : this.leaveService.createBatch({ items: leaveCreateOps.map((x) => x.payload) }).pipe(
            catchError((err) =>
              of<LeaveBatchCreateResult>(
                this.failedLeaveBatchResult(leaveCreateOps.length, err?.error?.detail ?? 'Leave batch request failed.'),
              ),
            ),
          ),
      wfhDelete: workDeleteOps.length === 0
        ? of([] as Array<{ row: CalendarDayRowVm; ok: boolean; message: string }>)
        : forkJoin(
            workDeleteOps.map((op) =>
              this.workLocationService.softDelete(op.id).pipe(
                map(() => ({ row: op.row, ok: true, message: '' })),
                catchError(() => of({ row: op.row, ok: false, message: 'Failed to remove existing work-location entry.' })),
              ),
            ),
          ),
      leaveDelete: leaveDeleteOps.length === 0
        ? of([] as Array<{ row: CalendarDayRowVm; ok: boolean; message: string }>)
        : forkJoin(
            leaveDeleteOps.map((op) =>
              this.leaveService.softDelete(op.id).pipe(
                map(() => ({ row: op.row, ok: true, message: '' })),
                catchError(() => of({ row: op.row, ok: false, message: 'Failed to remove existing leave entry.' })),
              ),
            ),
          ),
      wfhUpdate: workUpdateOps.length === 0
        ? of([] as Array<{ row: CalendarDayRowVm; ok: boolean; message: string }>)
        : forkJoin(
            workUpdateOps.map((op) =>
              this.workLocationService.update(op.id, op.payload).pipe(
                map(() => ({ row: op.row, ok: true, message: '' })),
                catchError(() => of({ row: op.row, ok: false, message: 'Failed to update existing work-location entry.' })),
              ),
            ),
          ),
      leaveUpdate: leaveUpdateOps.length === 0
        ? of([] as Array<{ row: CalendarDayRowVm; ok: boolean; message: string }>)
        : forkJoin(
            leaveUpdateOps.map((op) =>
              this.leaveService.update(op.id, op.payload).pipe(
                map(() => ({ row: op.row, ok: true, message: '' })),
                catchError(() => of({ row: op.row, ok: false, message: 'Failed to update existing leave entry.' })),
              ),
            ),
          ),
    })
      .pipe(
        map(({ wfhCreate, leaveCreate, wfhDelete, leaveDelete, wfhUpdate, leaveUpdate }) => {
          let skipped = 0;
          let failed = 0;
          const addedDates = new Set<string>();
          const updatedDates = new Set<string>();
          const recordAppliedDay = (row: CalendarDayRowVm): void => {
            if (row.originalCategory === 'none') {
              addedDates.add(row.dateIso);
            } else {
              updatedDates.add(row.dateIso);
            }
          };
          const recordUnappliedDay = (row: CalendarDayRowVm): void => {
            addedDates.delete(row.dateIso);
            updatedDates.delete(row.dateIso);
          };

          const wfhCreateByDate = new Map(workCreateOps.map((x) => [x.row.dateIso, x.row]));
          for (const result of wfhCreate.results) {
            const row = wfhCreateByDate.get(this.toDateKey(result.workDate));
            if (!row) {
              continue;
            }

            if (result.status === 'Created' && result.entry) {
              row.workLocationId = result.entry.id;
              recordAppliedDay(row);
              this.markApplied(row, `Created ${this.workLocationLabel(result.entry.workLocation)} entry.`);
              this.syncOriginalState(row);
              continue;
            }

            if (result.status === 'SkippedDuplicate') {
              skipped += 1;
              recordUnappliedDay(row);
              this.markSkipped(row, result.message ?? 'Skipped duplicate work-location entry.');
              continue;
            }

            failed += 1;
            recordUnappliedDay(row);
            this.markFailed(row, result.message ?? 'Work-location create failed.');
          }

          const leaveCreateByDate = new Map(leaveCreateOps.map((x) => [x.row.dateIso, x.row]));
          for (const result of leaveCreate.results) {
            const row = leaveCreateByDate.get(this.toDateKey(result.leaveDate));
            if (!row) {
              continue;
            }

            if (result.status === 'Created' && result.entry) {
              row.leaveId = result.entry.id;
              recordAppliedDay(row);
              this.markApplied(row, 'Created leave entry.');
              this.syncOriginalState(row);
              continue;
            }

            if (result.status === 'SkippedDuplicate') {
              skipped += 1;
              recordUnappliedDay(row);
              this.markSkipped(row, result.message ?? 'Skipped duplicate leave entry.');
              continue;
            }

            failed += 1;
            recordUnappliedDay(row);
            this.markFailed(row, result.message ?? 'Leave create failed.');
          }

          for (const result of wfhDelete) {
            if (result.ok) {
              result.row.workLocationId = null;
              recordAppliedDay(result.row);
              this.markApplied(result.row, 'Removed existing work-location entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              recordUnappliedDay(result.row);
              this.markFailed(result.row, result.message);
            }
          }

          for (const result of leaveDelete) {
            if (result.ok) {
              result.row.leaveId = null;
              recordAppliedDay(result.row);
              this.markApplied(result.row, 'Removed existing leave entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              recordUnappliedDay(result.row);
              this.markFailed(result.row, result.message);
            }
          }

          for (const result of wfhUpdate) {
            if (result.ok) {
              recordAppliedDay(result.row);
              this.markApplied(result.row, 'Updated existing work-location entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              recordUnappliedDay(result.row);
              this.markFailed(result.row, result.message);
            }
          }

          for (const result of leaveUpdate) {
            if (result.ok) {
              recordAppliedDay(result.row);
              this.markApplied(result.row, 'Updated existing leave entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              recordUnappliedDay(result.row);
              this.markFailed(result.row, result.message);
            }
          }

          return { skipped, failed, added: addedDates.size, updated: updatedDates.size };
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
          const holidayMap = new Map<string, string>();
          for (const holiday of holidays) {
            holidayMap.set(this.toDateKey(holiday.holidayDate), holiday.name);
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
    holidayMap: Map<string, string>,
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
      const holidayName = holidayMap.get(dateIso) ?? '';
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

  private toWfhMap(entries: WorkLocationEntry[]): Map<string, WorkLocationEntry> {
    const result = new Map<string, WorkLocationEntry>();
    for (const entry of entries) {
      result.set(this.toDateKey(entry.workDate), entry);
    }

    return result;
  }

  isRowChanged(row: CalendarDayRowVm): boolean {
    if (row.category !== row.originalCategory) {
      return true;
    }

    if (row.category === 'none') {
      return false;
    }

    if (row.entryType !== row.originalEntryType) {
      return true;
    }

    if (row.entryType === DayEntryType.SpecificHours) {
      return row.specificHours !== row.originalSpecificHours;
    }

    if (this.isLeaveCategory(row.category) && row.leaveType !== row.originalLeaveType) {
      return true;
    }

    return false;
  }

  private isWorkCategory(category: DayCategory): boolean {
    return category === 'wfh' || category === 'office';
  }

  private isLeaveCategory(category: DayCategory): boolean {
    return category === 'annual' || category === 'sick';
  }

  private requiresLeaveCreate(row: CalendarDayRowVm): boolean {
    return this.isLeaveCategory(row.category) && !this.isLeaveCategory(row.originalCategory);
  }

  private requiresLeaveDelete(row: CalendarDayRowVm): boolean {
    return this.isLeaveCategory(row.originalCategory) && !this.isLeaveCategory(row.category);
  }

  private requiresLeaveUpdate(row: CalendarDayRowVm): boolean {
    return this.isLeaveCategory(row.originalCategory) && this.isLeaveCategory(row.category) && this.isRowChanged(row);
  }

  private requiresWorkCreate(row: CalendarDayRowVm): boolean {
    return this.isWorkCategory(row.category) && !this.isWorkCategory(row.originalCategory);
  }

  private requiresWorkDelete(row: CalendarDayRowVm): boolean {
    return this.isWorkCategory(row.originalCategory) && !this.isWorkCategory(row.category);
  }

  private requiresWorkUpdate(row: CalendarDayRowVm): boolean {
    return this.isWorkCategory(row.originalCategory) && this.isWorkCategory(row.category) && this.isRowChanged(row);
  }

  private toWorkLocationType(category: DayCategory): WorkLocationType {
    return category === 'office' ? WorkLocationType.Office : WorkLocationType.Wfh;
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

  private workLocationLabel(workLocation: WorkLocationType): string {
    return workLocation === WorkLocationType.Office ? 'Office' : 'WFH';
  }

  private categoryLabel(category: Exclude<DayCategory, 'none'>): string {
    if (category === 'wfh') {
      return 'WFH';
    }

    if (category === 'office') {
      return 'Office';
    }

    return category === 'sick' ? 'Sick Leave' : 'Annual Leave';
  }

  private dayCountLabel(count: number): string {
    return `${count} ${count === 1 ? 'day' : 'days'}`;
  }

  private clearRowResult(row: CalendarDayRowVm): void {
    row.resultState = null;
    row.resultMessage = '';
  }

  private markApplied(row: CalendarDayRowVm, message: string): void {
    row.resultState = 'applied';
    row.resultMessage = message;
  }

  private markFailed(row: CalendarDayRowVm, message: string): void {
    row.resultState = 'failed';
    row.resultMessage = message;
  }

  private markSkipped(row: CalendarDayRowVm, message: string): void {
    row.resultState = 'skipped';
    row.resultMessage = message;
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
