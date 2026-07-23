import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { finalize, take } from 'rxjs';
import { DayEntrySummary, DayEntryType, LeaveEntry } from '../../models/api.models';
import { LeaveService } from '../../services/leave';
import { toLocalDateInputValue } from '../../utils/date';

@Component({
  selector: 'app-leave-management',
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './leave-management.html',
  styleUrl: './leave-management.scss',
})
export class LeaveManagement implements OnInit {
  private readonly leaveService = inject(LeaveService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns: string[] = ['leaveDate', 'entryType', 'hoursWorked', 'notes', 'actions'];
  readonly pageSizes = [10, 20, 50];
  readonly entryTypeOptions = [
    { value: DayEntryType.FullDay, label: 'Full Day' },
    { value: DayEntryType.HalfDay, label: 'Half Day' },
    { value: DayEntryType.SpecificHours, label: 'Specific Hours' },
  ];
  readonly summaryViewOptions = [
    { value: 'week', label: 'Week' },
    { value: 'month', label: 'Month' },
  ] as const;

  readonly entryForm = this.formBuilder.group({
    leaveDate: [this.today(), Validators.required],
    entryType: [DayEntryType.FullDay, Validators.required],
    specificHours: [null as number | null],
    notes: [''],
  });

  readonly summaryForm = this.formBuilder.group({
    view: ['month' as 'week' | 'month', Validators.required],
    date: [this.today(), Validators.required],
  });

  readonly filterForm = this.formBuilder.group({
    fromDate: [''],
    toDate: [''],
  });

  entries: LeaveEntry[] = [];
  pagedEntries: LeaveEntry[] = [];
  summary: DayEntrySummary | null = null;
  lastDeletedEntry: LeaveEntry | null = null;
  page = 1;
  pageSize = 10;
  isLoading = false;
  isSubmitting = false;
  isLoadingSummary = false;
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.updateSpecificHoursValidation(this.entryForm.value.entryType ?? DayEntryType.FullDay);
    this.loadEntries();
    this.loadSummary();
  }

  loadEntries(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const value = this.filterForm.getRawValue();

    this.leaveService
      .getAll({
        fromDate: value.fromDate || undefined,
        toDate: value.toDate || undefined,
      })
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (entries) => {
          this.entries = entries;
          this.page = 1;
          this.updatePagedEntries();
        },
        error: () => {
          this.errorMessage = 'Unable to load leave entries. Ensure the API is running.';
        },
      });
  }

  loadSummary(): void {
    const view = this.summaryForm.value.view;
    const date = this.summaryForm.value.date;

    if (!view || !date) {
      return;
    }

    this.isLoadingSummary = true;

    this.leaveService
      .getSummary(view, date)
      .pipe(
        take(1),
        finalize(() => {
          this.isLoadingSummary = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (summary) => {
          this.summary = summary;
        },
        error: () => {
          this.errorMessage = 'Unable to load leave summary.';
        },
      });
  }

  createEntry(): void {
    if (this.entryForm.invalid || this.isSubmitting) {
      this.entryForm.markAllAsTouched();
      return;
    }

    const value = this.entryForm.getRawValue();

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.leaveService
      .create({
        leaveDate: value.leaveDate ?? this.today(),
        entryType: value.entryType ?? DayEntryType.FullDay,
        specificHours: value.entryType === DayEntryType.SpecificHours ? value.specificHours : null,
        notes: value.notes?.trim() || null,
      })
      .pipe(
        take(1),
        finalize(() => {
          this.isSubmitting = false;
        }),
      )
      .subscribe({
        next: (created) => {
          this.entries = [created, ...this.entries].sort((a, b) => b.leaveDate.localeCompare(a.leaveDate));
          this.updatePagedEntries();
          this.entryForm.patchValue({
            leaveDate: this.today(),
            entryType: DayEntryType.FullDay,
            specificHours: null,
            notes: '',
          });
          this.lastDeletedEntry = null;
          this.infoMessage = 'Leave entry created.';
          this.loadSummary();
        },
        error: (err) => {
          this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to create leave entry.';
        },
      });
  }

  onEntryTypeChange(entryType: DayEntryType): void {
    this.updateSpecificHoursValidation(entryType);
  }

  softDeleteEntry(id: string): void {
    const entry = this.entries.find((x) => x.id === id);
    if (!entry) {
      return;
    }

    this.errorMessage = '';
    this.infoMessage = '';

    this.leaveService.softDelete(id).pipe(take(1)).subscribe({
      next: () => {
        this.lastDeletedEntry = entry;
        this.entries = this.entries.filter((x) => x.id !== id);
        this.updatePagedEntries();
        this.infoMessage = 'Leave entry deleted.';
        this.loadSummary();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to delete leave entry.';
      },
    });
  }

  undoDeleteEntry(): void {
    if (!this.lastDeletedEntry) {
      return;
    }

    const entry = this.lastDeletedEntry;

    this.leaveService.restore(entry.id).pipe(take(1)).subscribe({
      next: () => {
        this.entries = [entry, ...this.entries].sort((a, b) => b.leaveDate.localeCompare(a.leaveDate));
        this.updatePagedEntries();
        this.lastDeletedEntry = null;
        this.infoMessage = 'Leave entry restored.';
        this.loadSummary();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to restore leave entry.';
      },
    });
  }

  formatEntryType(entryType: DayEntryType): string {
    return this.entryTypeOptions.find((x) => x.value === entryType)?.label ?? 'Unknown';
  }

  applyFilters(): void {
    this.loadEntries();
  }

  clearFilters(): void {
    this.filterForm.patchValue({
      fromDate: '',
      toDate: '',
    });
    this.loadEntries();
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = Number(pageSize);
    this.page = 1;
    this.updatePagedEntries();
  }

  goToPreviousPage(): void {
    if (this.page > 1) {
      this.page -= 1;
      this.updatePagedEntries();
    }
  }

  goToNextPage(): void {
    if (this.page < this.totalPages) {
      this.page += 1;
      this.updatePagedEntries();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.entries.length / this.pageSize));
  }

  get rangeLabel(): string {
    if (this.entries.length === 0) {
      return '0 of 0';
    }

    const start = (this.page - 1) * this.pageSize + 1;
    const end = Math.min(this.page * this.pageSize, this.entries.length);
    return `${start}-${end} of ${this.entries.length}`;
  }

  get needsSpecificHours(): boolean {
    return this.entryForm.value.entryType === DayEntryType.SpecificHours;
  }

  private updateSpecificHoursValidation(entryType: DayEntryType): void {
    const control = this.entryForm.controls.specificHours;

    if (entryType === DayEntryType.SpecificHours) {
      control.setValidators([Validators.required, Validators.min(0.25), Validators.max(24)]);
    } else {
      control.clearValidators();
      control.setValue(null);
    }

    control.updateValueAndValidity();
  }

  private updatePagedEntries(): void {
    const start = (this.page - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.pagedEntries = this.entries.slice(start, end);
  }

  private today(): string {
    return toLocalDateInputValue();
  }
}