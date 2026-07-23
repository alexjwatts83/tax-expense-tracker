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

  entries: LeaveEntry[] = [];
  summary: DayEntrySummary | null = null;
  lastDeletedEntry: LeaveEntry | null = null;
  isLoading = false;
  isSubmitting = false;
  isLoadingSummary = false;
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.loadEntries();
    this.loadSummary();
  }

  loadEntries(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.leaveService
      .getAll()
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

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }
}