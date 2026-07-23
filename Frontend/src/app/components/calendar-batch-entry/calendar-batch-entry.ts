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
  CreateWorkFromHomeRequest,
  DayEntryType,
  LeaveEntry,
  LeaveBatchCreateResult,
  WorkFromHomeEntry,
  WorkFromHomeBatchCreateResult,
} from '../../models/api.models';
import { LeaveService } from '../../services/leave';
import { PublicHolidayService } from '../../services/public-holiday';
import { WorkFromHomeService } from '../../services/work-from-home';

type DayCategory = 'none' | 'wfh' | 'leave';

interface CalendarDayRowVm {
  dateIso: string;
  dayLabel: string;
  category: DayCategory;
  originalCategory: DayCategory;
  entryType: DayEntryType;
  originalEntryType: DayEntryType;
  specificHours: number | null;
  originalSpecificHours: number | null;
  leaveId: string | null;
  workFromHomeId: string | null;
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
  private readonly workFromHomeService = inject(WorkFromHomeService);
  private readonly leaveService = inject(LeaveService);

  readonly entryTypeOptions = [
    { value: DayEntryType.FullDay, label: 'Full Day' },
    { value: DayEntryType.HalfDay, label: 'Half Day' },
    { value: DayEntryType.SpecificHours, label: 'Specific Hours' },
  ];

  readonly categoryOptions: Array<{ value: DayCategory; label: string }> = [
    { value: 'none', label: 'None' },
    { value: 'wfh', label: 'WFH' },
    { value: 'leave', label: 'Leave' },
  ];

  readonly weekdayLabels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri'];

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

  get canBatchAdd(): boolean {
    return this.pendingCount > 0 && !this.hasValidationErrors;
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

  clearAll(): void {
    for (const row of this.rows) {
      if (row.isHoliday) {
        continue;
      }

      row.category = row.originalCategory;
      row.entryType = row.originalEntryType;
      row.specificHours = row.originalSpecificHours;
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
      if (row.entryType !== DayEntryType.SpecificHours) {
        row.specificHours = null;
      }
    }

    this.infoMessage = `All weekday rows were set to ${category === 'wfh' ? 'WFH' : 'Leave'}.`;
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

  onDayCellKeydown(row: CalendarDayRowVm, event: KeyboardEvent): void {
    if (event.target !== event.currentTarget || row.isHoliday) {
      return;
    }

    const key = event.key.toLowerCase();

    if (key === 'n') {
      this.onCategoryChange(row, 'none');
      event.preventDefault();
      return;
    }

    if (key === 'w') {
      this.onCategoryChange(row, 'wfh');
      event.preventDefault();
      return;
    }

    if (key === 'l') {
      this.onCategoryChange(row, 'leave');
      event.preventDefault();
      return;
    }

    if (row.category === 'none') {
      return;
    }

    if (key === 'f') {
      this.onEntryTypeChange(row, DayEntryType.FullDay);
      event.preventDefault();
      return;
    }

    if (key === 'h') {
      this.onEntryTypeChange(row, DayEntryType.HalfDay);
      event.preventDefault();
      return;
    }

    if (key === 's') {
      this.onEntryTypeChange(row, DayEntryType.SpecificHours);
      event.preventDefault();
    }
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

    if (!this.canBatchAdd) {
      this.errorMessage = 'Add at least one valid weekday selection before using Batch Add.';
      this.infoMessage = '';
      return;
    }

    const changedRows = this.rows.filter((row) => this.isRowChanged(row));

    const wfhCreateOps = changedRows
      .filter((row) => this.requiresCreate(row, 'wfh'))
      .map((row) => ({
        row,
        payload: {
          workDate: row.dateIso,
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateWorkFromHomeRequest,
      }));

    const leaveCreateOps = changedRows
      .filter((row) => this.requiresCreate(row, 'leave'))
      .map((row) => ({
        row,
        payload: {
          leaveDate: row.dateIso,
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateLeaveRequest,
      }));

    const wfhDeleteOps = changedRows
      .filter((row) => this.requiresDelete(row, 'wfh') && row.workFromHomeId)
      .map((row) => ({
        row,
        id: row.workFromHomeId as string,
      }));

    const leaveDeleteOps = changedRows
      .filter((row) => this.requiresDelete(row, 'leave') && row.leaveId)
      .map((row) => ({
        row,
        id: row.leaveId as string,
      }));

    const wfhUpdateOps = changedRows
      .filter((row) => this.requiresUpdate(row, 'wfh') && row.workFromHomeId)
      .map((row) => ({
        row,
        id: row.workFromHomeId as string,
        payload: {
          workDate: row.dateIso,
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateWorkFromHomeRequest,
      }));

    const leaveUpdateOps = changedRows
      .filter((row) => this.requiresUpdate(row, 'leave') && row.leaveId)
      .map((row) => ({
        row,
        id: row.leaveId as string,
        payload: {
          leaveDate: row.dateIso,
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
      wfhCreate: wfhCreateOps.length === 0
        ? of<WorkFromHomeBatchCreateResult>(this.emptyWfhBatchResult())
        : this.workFromHomeService.createBatch({ items: wfhCreateOps.map((x) => x.payload) }).pipe(
            catchError((err) =>
              of<WorkFromHomeBatchCreateResult>(
                this.failedWfhBatchResult(wfhCreateOps.length, err?.error?.detail ?? 'WFH batch request failed.'),
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
      wfhDelete: wfhDeleteOps.length === 0
        ? of([] as Array<{ row: CalendarDayRowVm; ok: boolean; message: string }>)
        : forkJoin(
            wfhDeleteOps.map((op) =>
              this.workFromHomeService.softDelete(op.id).pipe(
                map(() => ({ row: op.row, ok: true, message: '' })),
                catchError(() => of({ row: op.row, ok: false, message: 'Failed to remove existing WFH entry.' })),
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
      wfhUpdate: wfhUpdateOps.length === 0
        ? of([] as Array<{ row: CalendarDayRowVm; ok: boolean; message: string }>)
        : forkJoin(
            wfhUpdateOps.map((op) =>
              this.workFromHomeService.update(op.id, op.payload).pipe(
                map(() => ({ row: op.row, ok: true, message: '' })),
                catchError(() => of({ row: op.row, ok: false, message: 'Failed to update existing WFH entry.' })),
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
          let applied = 0;
          let skipped = 0;
          let failed = 0;

          const wfhCreateByDate = new Map(wfhCreateOps.map((x) => [x.row.dateIso, x.row]));
          for (const result of wfhCreate.results) {
            const row = wfhCreateByDate.get(result.workDate);
            if (!row) {
              continue;
            }

            if (result.status === 'Created' && result.entry) {
              applied += 1;
              row.workFromHomeId = result.entry.id;
              this.markApplied(row, 'Created WFH entry.');
              this.syncOriginalState(row);
              continue;
            }

            if (result.status === 'SkippedDuplicate') {
              skipped += 1;
              this.markSkipped(row, result.message ?? 'Skipped duplicate WFH entry.');
              continue;
            }

            failed += 1;
            this.markFailed(row, result.message ?? 'WFH create failed.');
          }

          const leaveCreateByDate = new Map(leaveCreateOps.map((x) => [x.row.dateIso, x.row]));
          for (const result of leaveCreate.results) {
            const row = leaveCreateByDate.get(result.leaveDate);
            if (!row) {
              continue;
            }

            if (result.status === 'Created' && result.entry) {
              applied += 1;
              row.leaveId = result.entry.id;
              this.markApplied(row, 'Created leave entry.');
              this.syncOriginalState(row);
              continue;
            }

            if (result.status === 'SkippedDuplicate') {
              skipped += 1;
              this.markSkipped(row, result.message ?? 'Skipped duplicate leave entry.');
              continue;
            }

            failed += 1;
            this.markFailed(row, result.message ?? 'Leave create failed.');
          }

          for (const result of wfhDelete) {
            if (result.ok) {
              applied += 1;
              result.row.workFromHomeId = null;
              this.markApplied(result.row, 'Removed existing WFH entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              this.markFailed(result.row, result.message);
            }
          }

          for (const result of leaveDelete) {
            if (result.ok) {
              applied += 1;
              result.row.leaveId = null;
              this.markApplied(result.row, 'Removed existing leave entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              this.markFailed(result.row, result.message);
            }
          }

          for (const result of wfhUpdate) {
            if (result.ok) {
              applied += 1;
              this.markApplied(result.row, 'Updated existing WFH entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              this.markFailed(result.row, result.message);
            }
          }

          for (const result of leaveUpdate) {
            if (result.ok) {
              applied += 1;
              this.markApplied(result.row, 'Updated existing leave entry.');
              this.syncOriginalState(result.row);
            } else {
              failed += 1;
              this.markFailed(result.row, result.message);
            }
          }

          return { applied, skipped, failed };
        }),
        finalize(() => {
          this.isSubmitting = false;
        }),
      )
      .subscribe({
        next: ({ applied, skipped, failed }) => {
          this.infoMessage = `Batch apply complete: Applied ${applied}, Skipped ${skipped}, Failed ${failed}.`;
          this.errorMessage = failed > 0 ? 'Some rows failed. Fix the highlighted rows and submit again.' : '';

          if (failed === 0) {
            this.loadMonth();
          }
        },
        error: () => {
          this.errorMessage = 'Unable to submit batch changes.';
          this.infoMessage = '';
        },
      });
  }

  private emptyWfhBatchResult(): WorkFromHomeBatchCreateResult {
    return {
      totalRequested: 0,
      createdCount: 0,
      skippedCount: 0,
      failedCount: 0,
      results: [],
    };
  }

  private failedWfhBatchResult(totalRequested: number, message: string): WorkFromHomeBatchCreateResult {
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

  private loadMonth(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    const fromDate = this.toDateIso(this.monthAnchor);
    const toDate = this.toDateIso(this.endOfMonth(this.monthAnchor));

    forkJoin({
      holidays: this.publicHolidayService.getAll({ fromDate, toDate }),
      leave: this.leaveService.getAll({ fromDate, toDate }),
      wfh: this.workFromHomeService.getAll({ fromDate, toDate }),
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
            holidayMap.set(holiday.holidayDate, holiday.name);
          }

          const leaveByDate = this.toLeaveMap(leave);
          const wfhByDate = this.toWfhMap(wfh);

          this.rows = this.buildWeekdayRows(this.monthAnchor, holidayMap, leaveByDate, wfhByDate);
        },
        error: () => {
          this.rows = this.buildWeekdayRows(
            this.monthAnchor,
            new Map<string, string>(),
            new Map<string, LeaveEntry>(),
            new Map<string, WorkFromHomeEntry>(),
          );
          this.errorMessage = 'Unable to load public holidays. Holiday day-locking may be incomplete until API is available.';
        },
      });
  }

  private buildWeekdayRows(
    anchor: Date,
    holidayMap: Map<string, string>,
    leaveByDate: Map<string, LeaveEntry>,
    wfhByDate: Map<string, WorkFromHomeEntry>,
  ): CalendarDayRowVm[] {
    const rows: CalendarDayRowVm[] = [];
    const year = anchor.getFullYear();
    const month = anchor.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

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

      const originalCategory: DayCategory = leaveEntry ? 'leave' : wfhEntry ? 'wfh' : 'none';
      const originalEntryType = leaveEntry?.entryType ?? wfhEntry?.entryType ?? DayEntryType.FullDay;
      const originalSpecificHours =
        originalEntryType === DayEntryType.SpecificHours
          ? (leaveEntry?.hoursWorked ?? wfhEntry?.hoursWorked ?? null)
          : null;

      rows.push({
        dateIso,
        dayLabel: new Intl.DateTimeFormat('en-AU', { weekday: 'short', day: '2-digit', month: 'short' }).format(date),
        category: originalCategory,
        originalCategory,
        entryType: originalEntryType,
        originalEntryType,
        specificHours: originalSpecificHours,
        originalSpecificHours,
        leaveId: leaveEntry?.id ?? null,
        workFromHomeId: wfhEntry?.id ?? null,
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
      result.set(entry.leaveDate, entry);
    }

    return result;
  }

  private toWfhMap(entries: WorkFromHomeEntry[]): Map<string, WorkFromHomeEntry> {
    const result = new Map<string, WorkFromHomeEntry>();
    for (const entry of entries) {
      result.set(entry.workDate, entry);
    }

    return result;
  }

  private isRowChanged(row: CalendarDayRowVm): boolean {
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

    return false;
  }

  private requiresCreate(row: CalendarDayRowVm, target: Exclude<DayCategory, 'none'>): boolean {
    return row.category === target && row.originalCategory !== target;
  }

  private requiresDelete(row: CalendarDayRowVm, original: Exclude<DayCategory, 'none'>): boolean {
    return row.originalCategory === original && row.category !== original;
  }

  private requiresUpdate(row: CalendarDayRowVm, category: Exclude<DayCategory, 'none'>): boolean {
    return row.originalCategory === category && row.category === category && this.isRowChanged(row);
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

    if (row.category === 'none') {
      row.leaveId = null;
      row.workFromHomeId = null;
    }
  }
}
