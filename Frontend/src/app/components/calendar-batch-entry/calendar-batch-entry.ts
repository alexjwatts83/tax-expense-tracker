import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, take } from 'rxjs';
import { DayEntryType } from '../../models/api.models';
import { PublicHolidayService } from '../../services/public-holiday';

type DayCategory = 'none' | 'wfh' | 'leave';

interface CalendarDayRowVm {
  dateIso: string;
  dayLabel: string;
  category: DayCategory;
  entryType: DayEntryType;
  specificHours: number | null;
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
    MatSelectModule,
  ],
  templateUrl: './calendar-batch-entry.html',
  styleUrl: './calendar-batch-entry.scss',
})
export class CalendarBatchEntry implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly publicHolidayService = inject(PublicHolidayService);

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
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.loadMonth();
  }

  get monthLabel(): string {
    return new Intl.DateTimeFormat('en-AU', { month: 'long', year: 'numeric' }).format(this.monthAnchor);
  }

  get pendingCount(): number {
    return this.rows.filter((row) => row.category !== 'none').length;
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

      row.category = 'none';
      row.entryType = DayEntryType.FullDay;
      row.specificHours = null;
    }

    this.infoMessage = 'All pending selections were cleared.';
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
    if (!this.canBatchAdd) {
      this.errorMessage = 'Add at least one valid weekday selection before using Batch Add.';
      this.infoMessage = '';
      return;
    }

    this.errorMessage = '';
    this.infoMessage =
      'Batch payload is ready. API submission will be connected in the next phase using separate WFH and Leave endpoints.';
  }

  private loadMonth(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    const fromDate = this.toDateIso(this.monthAnchor);
    const toDate = this.toDateIso(this.endOfMonth(this.monthAnchor));

    this.publicHolidayService
      .getAll({ fromDate, toDate })
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (holidays) => {
          const holidayMap = new Map<string, string>();
          for (const holiday of holidays) {
            holidayMap.set(holiday.holidayDate, holiday.name);
          }

          this.rows = this.buildWeekdayRows(this.monthAnchor, holidayMap);
        },
        error: () => {
          this.rows = this.buildWeekdayRows(this.monthAnchor, new Map<string, string>());
          this.errorMessage = 'Unable to load public holidays. Holiday day-locking may be incomplete until API is available.';
        },
      });
  }

  private buildWeekdayRows(anchor: Date, holidayMap: Map<string, string>): CalendarDayRowVm[] {
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

      rows.push({
        dateIso,
        dayLabel: new Intl.DateTimeFormat('en-AU', { weekday: 'short', day: '2-digit', month: 'short' }).format(date),
        category: 'none',
        entryType: DayEntryType.FullDay,
        specificHours: null,
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
}
