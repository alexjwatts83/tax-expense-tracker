import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { Tag, Tracker } from '../../models/api.models';
import { ExpenseService } from '../../services/expense';
import { TagService } from '../../services/tag';
import { TrackerService } from '../../services/tracker';

@Component({
  selector: 'app-expense-form',
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './expense-form.html',
  styleUrl: './expense-form.scss',
})
export class ExpenseForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly expenseService = inject(ExpenseService);
  private readonly trackerService = inject(TrackerService);
  private readonly tagService = inject(TagService);
  private readonly router = inject(Router);

  readonly form: FormGroup;
  trackers: Tracker[] = [];
  tags: Tag[] = [];
  isLoadingLookups = false;
  isSubmitting = false;
  errorMessage = '';

  constructor() {
    this.form = this.formBuilder.group({
      item: ['', [Validators.required]],
      description: [''],
      date: ['', [Validators.required]],
      bank: ['', [Validators.required]],
      price: [0, [Validators.required, Validators.min(0)]],
      sourceId: ['', [Validators.required]],
      tagIds: [[]],
    });
  }

  ngOnInit(): void {
    this.loadLookups();
  }

  loadLookups(): void {
    this.isLoadingLookups = true;
    this.errorMessage = '';

    this.trackerService.getAll().subscribe({
      next: (trackers) => {
        this.trackers = trackers;

        this.tagService.getAll().subscribe({
          next: (tags) => {
            this.tags = tags;
            this.isLoadingLookups = false;
          },
          error: () => {
            this.errorMessage = 'Unable to load tags.';
            this.isLoadingLookups = false;
          },
        });
      },
      error: () => {
        this.errorMessage = 'Unable to load trackers.';
        this.isLoadingLookups = false;
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.isSubmitting) {
      return;
    }

    const payload = {
      item: this.form.value.item?.trim() ?? '',
      description: this.form.value.description?.trim() ?? '',
      date: this.form.value.date,
      bank: this.form.value.bank?.trim() ?? '',
      price: Number(this.form.value.price),
      sourceId: this.form.value.sourceId,
      tagIds: this.form.value.tagIds ?? [],
    };

    this.isSubmitting = true;
    this.errorMessage = '';

    this.expenseService.create(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/expenses']);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to save expense.';
        this.isSubmitting = false;
      },
    });
  }
}
