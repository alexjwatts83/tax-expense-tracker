import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import { forkJoin, of, switchMap } from 'rxjs';
import { Bank, Tag, Tracker } from '../../models/api.models';
import { BankService } from '../../services/bank';
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
  private readonly bankService = inject(BankService);
  private readonly trackerService = inject(TrackerService);
  private readonly tagService = inject(TagService);
  private readonly router = inject(Router);

  readonly form: FormGroup;
  banks: Bank[] = [];
  trackers: Tracker[] = [];
  tags: Tag[] = [];
  isLoadingLookups = false;
  isSubmitting = false;
  errorMessage = '';

  constructor() {
    this.form = this.formBuilder.group({
      description: [''],
      date: ['', [Validators.required]],
      bankId: ['', [Validators.required]],
      price: [0, [Validators.required, Validators.min(0)]],
      sourceId: ['', [Validators.required]],
      tagIds: [[]],
      manualTags: [''],
    });
  }

  ngOnInit(): void {
    this.loadLookups();
  }

  loadLookups(): void {
    this.isLoadingLookups = true;
    this.errorMessage = '';

    forkJoin({
      banks: this.bankService.getAll(),
      trackers: this.trackerService.getAll(),
      tags: this.tagService.getAll(),
    }).subscribe({
      next: ({ banks, trackers, tags }) => {
        this.banks = banks;
        this.trackers = trackers;
        this.tags = tags;
        this.isLoadingLookups = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load lookup data.';
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

    this.isSubmitting = true;
    this.errorMessage = '';

    this.applyManualTagsInternal()
      .pipe(
        switchMap((tagIds) => {
          const payload = {
            description: this.form.value.description?.trim() ?? '',
            date: this.form.value.date,
            bankId: this.form.value.bankId,
            price: Number(this.form.value.price),
            sourceId: this.form.value.sourceId,
            tagIds,
          };

          return this.expenseService.create(payload);
        }),
      )
      .subscribe({
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

  applyManualTags(): void {
    this.errorMessage = '';

    this.applyManualTagsInternal().subscribe({
      next: (tagIds) => {
        this.form.patchValue({ tagIds, manualTags: '' });
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to apply manual tags.';
      },
    });
  }

  private parseManualTags(rawValue: string | null | undefined): string[] {
    if (!rawValue) {
      return [];
    }

    const unique = new Map<string, string>();
    for (const token of rawValue.split(',')) {
      const trimmed = token.trim();
      if (!trimmed) {
        continue;
      }

      const key = trimmed.toLowerCase();
      if (!unique.has(key)) {
        unique.set(key, trimmed);
      }
    }

    return [...unique.values()];
  }

  private resolveTagIds(selectedTagIds: string[], manualTagNames: string[]) {
    if (manualTagNames.length === 0) {
      return of([...new Set(selectedTagIds)]);
    }

    const existingByName = new Map(this.tags.map((tag) => [tag.name.trim().toLowerCase(), tag]));
    const existingIds = manualTagNames
      .map((name) => existingByName.get(name.toLowerCase())?.id)
      .filter((id): id is string => !!id);

    const namesToCreate = manualTagNames.filter((name) => !existingByName.has(name.toLowerCase()));
    if (namesToCreate.length === 0) {
      return of([...new Set([...selectedTagIds, ...existingIds])]);
    }

    return forkJoin(namesToCreate.map((name) => this.tagService.create({ name }))).pipe(
      switchMap((createdTags) => {
        this.tags = [...this.tags, ...createdTags].sort((a, b) => a.name.localeCompare(b.name));
        const createdIds = createdTags.map((tag) => tag.id);
        return of([...new Set([...selectedTagIds, ...existingIds, ...createdIds])]);
      }),
    );
  }

  private applyManualTagsInternal() {
    const selectedTagIds = (this.form.value.tagIds ?? []) as string[];
    const manualTagNames = this.parseManualTags(this.form.value.manualTags);
    return this.resolveTagIds(selectedTagIds, manualTagNames);
  }
}
