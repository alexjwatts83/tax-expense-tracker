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
  }

  onEntryTypeChange(row: CalendarDayRowVm, entryType: DayEntryType): void {
    row.entryType = entryType;

    if (entryType !== DayEntryType.SpecificHours) {
      row.specificHours = null;
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

    const wfhCreates: CreateWorkFromHomeRequest[] = changedRows
      .filter((row) => this.requiresCreate(row, 'wfh'))
      .map((row) => ({
        workDate: row.dateIso,
        entryType: row.entryType,
        specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
        notes: null,
      }));

    const leaveCreates: CreateLeaveRequest[] = changedRows
      .filter((row) => this.requiresCreate(row, 'leave'))
      .map((row) => ({
        leaveDate: row.dateIso,
        entryType: row.entryType,
        specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
        notes: null,
      }));

    const wfhDeletes = changedRows
      .filter((row) => this.requiresDelete(row, 'wfh') && row.workFromHomeId)
      .map((row) => row.workFromHomeId as string);

    const leaveDeletes = changedRows
      .filter((row) => this.requiresDelete(row, 'leave') && row.leaveId)
      .map((row) => row.leaveId as string);

    const wfhUpdates = changedRows
      .filter((row) => this.requiresUpdate(row, 'wfh') && row.workFromHomeId)
      .map((row) => ({
        id: row.workFromHomeId as string,
        payload: {
          workDate: row.dateIso,
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateWorkFromHomeRequest,
      }));

    const leaveUpdates = changedRows
      .filter((row) => this.requiresUpdate(row, 'leave') && row.leaveId)
      .map((row) => ({
        id: row.leaveId as string,
        payload: {
          leaveDate: row.dateIso,
          entryType: row.entryType,
          specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
          notes: null,
        } as CreateLeaveRequest,
      }));

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    forkJoin({
      wfhCreate: wfhCreates.length === 0
        ? of<WorkFromHomeBatchCreateResult>(this.emptyWfhBatchResult())
        : this.workFromHomeService.createBatch({ items: wfhCreates }).pipe(
            catchError((err) =>
              of<WorkFromHomeBatchCreateResult>(
                this.failedWfhBatchResult(wfhCreates.length, err?.error?.detail ?? 'WFH batch request failed.'),
              ),
            ),
          ),
      leaveCreate: leaveCreates.length === 0
        ? of<LeaveBatchCreateResult>(this.emptyLeaveBatchResult())
        : this.leaveService.createBatch({ items: leaveCreates }).pipe(
            catchError((err) =>
              of<LeaveBatchCreateResult>(
                this.failedLeaveBatchResult(leaveCreates.length, err?.error?.detail ?? 'Leave batch request failed.'),
              ),
            ),
          ),
      wfhDelete: wfhDeletes.length === 0
        ? of({ success: 0, failed: 0 })
        : forkJoin(
            wfhDeletes.map((id) =>
              this.workFromHomeService.softDelete(id).pipe(
                map(() => true),
                catchError(() => of(false)),
              ),
            ),
          ).pipe(
            map((results) => ({
              success: results.filter((x) => x).length,
              failed: results.filter((x) => !x).length,
            })),
          ),
      leaveDelete: leaveDeletes.length === 0
        ? of({ success: 0, failed: 0 })
        : forkJoin(
            leaveDeletes.map((id) =>
              this.leaveService.softDelete(id).pipe(
                map(() => true),
                catchError(() => of(false)),
              ),
            ),
          ).pipe(
            map((results) => ({
              success: results.filter((x) => x).length,
              failed: results.filter((x) => !x).length,
            })),
          ),
      wfhUpdate: wfhUpdates.length === 0
        ? of({ success: 0, failed: 0 })
        : forkJoin(
            wfhUpdates.map((x) =>
              this.workFromHomeService.update(x.id, x.payload).pipe(
                map(() => true),
                catchError(() => of(false)),
              ),
            ),
          ).pipe(
            map((results) => ({
              success: results.filter((x) => x).length,
              failed: results.filter((x) => !x).length,
            })),
          ),
      leaveUpdate: leaveUpdates.length === 0
        ? of({ success: 0, failed: 0 })
        : forkJoin(
            leaveUpdates.map((x) =>
              this.leaveService.update(x.id, x.payload).pipe(
                map(() => true),
                catchError(() => of(false)),
              ),
            ),
          ).pipe(
            map((results) => ({
              success: results.filter((x) => x).length,
              failed: results.filter((x) => !x).length,
            })),
          ),
    })
      .pipe(
        map(({ wfhCreate, leaveCreate, wfhDelete, leaveDelete, wfhUpdate, leaveUpdate }) => {
          const safeWfhCreate = wfhCreate ?? this.emptyWfhBatchResult();
          const safeLeaveCreate = leaveCreate ?? this.emptyLeaveBatchResult();

          const applied =
            safeWfhCreate.createdCount +
            safeLeaveCreate.createdCount +
            wfhDelete.success +
            leaveDelete.success +
            wfhUpdate.success +
            leaveUpdate.success;
          const skipped = safeWfhCreate.skippedCount + safeLeaveCreate.skippedCount;
          const failed =
            safeWfhCreate.failedCount +
            safeLeaveCreate.failedCount +
            wfhDelete.failed +
            leaveDelete.failed +
            wfhUpdate.failed +
            leaveUpdate.failed;
          return { applied, skipped, failed };
        }),
        finalize(() => {
          this.isSubmitting = false;
        }),
      )
      .subscribe({
        next: ({ applied, skipped, failed }) => {
          this.infoMessage = `Batch apply complete: Applied ${applied}, Skipped ${skipped}, Failed ${failed}.`;
          this.errorMessage = failed > 0 ? 'Some rows failed. Adjust your selections and submit again.' : '';
          this.loadMonth();
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
}
