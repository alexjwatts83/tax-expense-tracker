import { CommonModule, CurrencyPipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { catchError, finalize, forkJoin, of, take } from 'rxjs';
import { DayEntryType, ExpenseSummary, LeaveEntry, LeaveType, WorkLocationEntry, WorkLocationType } from '../../models/api.models';
import { ExpenseService } from '../../services/expense';
import { LeaveService } from '../../services/leave';
import { WorkLocationService } from '../../services/work-location';
import { StandardDateDisplayPipe } from '../../shared/standard-date-display.pipe';

interface TimeRecordRow {
  date: string;
  category: 'Work Location' | 'Leave';
  type: string;
  entryType: string;
  hours: number;
  notes: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, CurrencyPipe, MatCardModule, MatProgressSpinnerModule, MatTableModule, StandardDateDisplayPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly expenseService = inject(ExpenseService);
  private readonly workLocationService = inject(WorkLocationService);
  private readonly leaveService = inject(LeaveService);
  private readonly cdr = inject(ChangeDetectorRef);

  summary: ExpenseSummary | null = null;
  allTimeRecords: TimeRecordRow[] = [];
  recentTimeRecords: TimeRecordRow[] = [];
  readonly timeRecordColumns: string[] = ['date', 'category', 'type', 'entryType', 'hours', 'notes'];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      summary: this.expenseService.getSummary().pipe(catchError(() => of(null))),
      workLocations: this.workLocationService.getAll().pipe(catchError(() => of([] as WorkLocationEntry[]))),
      leaveEntries: this.leaveService.getAll().pipe(catchError(() => of([] as LeaveEntry[]))),
    })
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: ({ summary, workLocations, leaveEntries }) => {
          this.summary = summary;
          this.allTimeRecords = this.buildTimeRecords(workLocations, leaveEntries);
          this.recentTimeRecords = this.allTimeRecords.slice(0, 8);
        },
        error: () => {
          this.errorMessage = 'Unable to load dashboard summary. Ensure the API is running.';
        },
      });
  }

  get topTracker(): string {
    return this.summary?.bySource[0]?.source ?? 'Not available';
  }

  get topBank(): string {
    return this.summary?.byBank[0]?.bank ?? 'Not available';
  }

  get totalTimeRecords(): number {
    return this.allTimeRecords.length;
  }

  get totalTimeHours(): number {
    return this.allTimeRecords.reduce((total, record) => total + record.hours, 0);
  }

  get currentMonthHours(): number {
    const now = new Date();
    const monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
    return this.allTimeRecords
      .filter((record) => this.toDate(record.date) >= monthStart)
      .reduce((total, record) => total + record.hours, 0);
  }

  get leaveRecordCount(): number {
    return this.allTimeRecords.filter((record) => record.category === 'Leave').length;
  }

  private buildTimeRecords(workLocations: WorkLocationEntry[], leaveEntries: LeaveEntry[]): TimeRecordRow[] {
    const workLocationRows: TimeRecordRow[] = workLocations.map((entry) => ({
      date: entry.workDate,
      category: 'Work Location',
      type: entry.workLocation === WorkLocationType.Wfh ? 'WFH' : 'Office',
      entryType: this.formatEntryType(entry.entryType),
      hours: entry.hoursWorked,
      notes: entry.notes?.trim() || '-',
    }));

    const leaveRows: TimeRecordRow[] = leaveEntries.map((entry) => ({
      date: entry.leaveDate,
      category: 'Leave',
      type: entry.leaveType === LeaveType.Annual ? 'Annual' : 'Sick',
      entryType: this.formatEntryType(entry.entryType),
      hours: entry.hoursWorked,
      notes: entry.notes?.trim() || '-',
    }));

    return [...workLocationRows, ...leaveRows].sort((a, b) => b.date.localeCompare(a.date));
  }

  private formatEntryType(entryType: DayEntryType): string {
    switch (entryType) {
      case DayEntryType.FullDay:
        return 'Full Day';
      case DayEntryType.HalfDay:
        return 'Half Day';
      case DayEntryType.SpecificHours:
        return 'Specific Hours';
      default:
        return 'Unknown';
    }
  }

  private toDate(dateValue: string): Date {
    const [year, month, day] = dateValue.slice(0, 10).split('-').map(Number);
    return new Date(year ?? 0, (month ?? 1) - 1, day ?? 1);
  }
}
