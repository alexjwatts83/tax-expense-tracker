import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { finalize, take } from 'rxjs';
import { Tracker } from '../../models/api.models';
import { TrackerService } from '../../services/tracker';

@Component({
  selector: 'app-tracker-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './tracker-management.html',
  styleUrl: './tracker-management.scss',
})
export class TrackerManagement implements OnInit {
  private readonly trackerService = inject(TrackerService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns: string[] = ['name', 'description', 'createdAt', 'actions'];

  readonly trackerForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
  });

  trackers: Tracker[] = [];
  lastDeletedTracker: Tracker | null = null;
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.loadTrackers();
  }

  loadTrackers(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.trackerService
      .getAll()
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (trackers) => {
          this.trackers = trackers;
        },
        error: () => {
          this.errorMessage = 'Unable to load trackers. Ensure the API is running.';
        },
      });
  }

  createTracker(): void {
    if (this.trackerForm.invalid || this.isSubmitting) {
      this.trackerForm.markAllAsTouched();
      return;
    }

    const name = this.trackerForm.value.name?.trim() ?? '';
    const description = this.trackerForm.value.description?.trim() ?? '';

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.trackerService
      .create({
        name,
        description: description.length > 0 ? description : undefined,
      })
      .subscribe({
        next: (created) => {
          this.trackers = [created, ...this.trackers];
          this.trackerForm.reset();
          this.lastDeletedTracker = null;
          this.infoMessage = 'Tracker created.';
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to create tracker.';
          this.isSubmitting = false;
        },
      });
  }

  softDeleteTracker(id: string): void {
    this.errorMessage = '';
    this.infoMessage = '';

    const tracker = this.trackers.find((x) => x.id === id);
    if (!tracker) {
      return;
    }

    this.trackerService.softDelete(id).subscribe({
      next: () => {
        this.lastDeletedTracker = tracker;
        this.trackers = this.trackers.filter((tracker) => tracker.id !== id);
        this.infoMessage = `Tracker "${tracker.name}" deleted.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to delete tracker.';
      },
    });
  }

  undoDeleteTracker(): void {
    if (!this.lastDeletedTracker) {
      return;
    }

    const tracker = this.lastDeletedTracker;
    this.errorMessage = '';
    this.infoMessage = '';

    this.trackerService.restore(tracker.id).subscribe({
      next: () => {
        this.trackers = [tracker, ...this.trackers].sort((a, b) => a.name.localeCompare(b.name));
        this.lastDeletedTracker = null;
        this.infoMessage = `Tracker "${tracker.name}" restored.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to restore tracker.';
      },
    });
  }
}
