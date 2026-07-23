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
import { DayEntrySummary, DayEntryType, WorkFromHomeEntry } from '../../models/api.models';
import { WorkFromHomeService } from '../../services/work-from-home';
import { toLocalDateInputValue } from '../../utils/date';

@Component({
  selector: 'app-work-from-home-management',
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
  templateUrl: './work-from-home-management.html',
  styleUrl: './work-from-home-management.scss',
})
export class WorkFromHomeManagement implements OnInit {
  private readonly workFromHomeService = inject(WorkFromHomeService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns: string[] = ['workDate', 'entryType', 'hoursWorked', 'notes', 'actions'];
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
    workDate: [this.today(), Validators.required],
    entryType: [DayEntryType.FullDay, Validators.required],
    specificHours: [null as number | null],
    notes: [''],
  });

  readonly summaryForm = this.formBuilder.group({
    view: ['month' as 'week' | 'month', Validators.required],
    date: [this.today(), Validators.required],
  });

  entries: WorkFromHomeEntry[] = [];
  summary: DayEntrySummary | null = null;
  lastDeletedEntry: WorkFromHomeEntry | null = null;
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

    this.workFromHomeService
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
          this.errorMessage = 'Unable to load work-from-home entries. Ensure the API is running.';
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

    this.workFromHomeService
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
          this.errorMessage = 'Unable to load work-from-home summary.';
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

    this.workFromHomeService
      .create({
        workDate: value.workDate ?? this.today(),
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
          this.entries = [created, ...this.entries].sort((a, b) => b.workDate.localeCompare(a.workDate));
          this.entryForm.patchValue({
            workDate: this.today(),
            entryType: DayEntryType.FullDay,
            specificHours: null,
            notes: '',
          });
          this.lastDeletedEntry = null;
          this.infoMessage = 'Work-from-home entry created.';
          this.loadSummary();
        },
        error: (err) => {
          this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to create work-from-home entry.';
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

    this.workFromHomeService.softDelete(id).pipe(take(1)).subscribe({
      next: () => {
        this.lastDeletedEntry = entry;
        this.entries = this.entries.filter((x) => x.id !== id);
        this.infoMessage = 'Work-from-home entry deleted.';
        this.loadSummary();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to delete work-from-home entry.';
      },
    });
  }

  undoDeleteEntry(): void {
    if (!this.lastDeletedEntry) {
      return;
    }

    const entry = this.lastDeletedEntry;

    this.workFromHomeService.restore(entry.id).pipe(take(1)).subscribe({
      next: () => {
        this.entries = [entry, ...this.entries].sort((a, b) => b.workDate.localeCompare(a.workDate));
        this.lastDeletedEntry = null;
        this.infoMessage = 'Work-from-home entry restored.';
        this.loadSummary();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to restore work-from-home entry.';
      },
    });
  }

  formatEntryType(entryType: DayEntryType): string {
    return this.entryTypeOptions.find((x) => x.value === entryType)?.label ?? 'Unknown';
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

  private today(): string {
    return toLocalDateInputValue();
  }
}