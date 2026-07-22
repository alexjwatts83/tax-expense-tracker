import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
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

  readonly displayedColumns: string[] = ['name', 'description', 'createdAt', 'actions'];

  readonly trackerForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]],
  });

  trackers: Tracker[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadTrackers();
  }

  loadTrackers(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.trackerService.getAll().subscribe({
      next: (trackers) => {
        this.trackers = trackers;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load trackers. Ensure the API is running.';
        this.isLoading = false;
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

    this.trackerService
      .create({
        name,
        description: description.length > 0 ? description : undefined,
      })
      .subscribe({
        next: (created) => {
          this.trackers = [created, ...this.trackers];
          this.trackerForm.reset();
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

    this.trackerService.softDelete(id).subscribe({
      next: () => {
        this.trackers = this.trackers.filter((tracker) => tracker.id !== id);
      },
      error: () => {
        this.errorMessage = 'Unable to delete tracker.';
      },
    });
  }
}
