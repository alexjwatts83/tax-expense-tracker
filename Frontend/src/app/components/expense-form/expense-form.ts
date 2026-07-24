import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, of, switchMap, take } from 'rxjs';
import { Bank, Expense, Tag, Tracker } from '../../models/api.models';
import { BankService } from '../../services/bank';
import { ExpenseService } from '../../services/expense';
import { TagService } from '../../services/tag';
import { TrackerService } from '../../services/tracker';
import { DateInputDirective } from '../../shared/date-input.directive';
import { DatePickerToggleComponent } from '../../shared/date-picker-toggle.component';

@Component({
  selector: 'app-expense-form',
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ReactiveFormsModule,
    RouterLink,
    DateInputDirective,
    DatePickerToggleComponent,
  ],
  templateUrl: './expense-form.html',
  styleUrl: './expense-form.scss',
})
export class ExpenseForm implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly formBuilder = inject(FormBuilder);
  private readonly expenseService = inject(ExpenseService);
  private readonly bankService = inject(BankService);
  private readonly trackerService = inject(TrackerService);
  private readonly tagService = inject(TagService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly form: FormGroup;
  banks: Bank[] = [];
  trackers: Tracker[] = [];
  tags: Tag[] = [];
  isLoadingLookups = false;
  isLoadingExpense = false;
  isSubmitting = false;
  errorMessage = '';
  isEditMode = false;
  private expenseId: string | null = null;

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
    this.expenseId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.expenseId;

    if (this.expenseId) {
      this.loadExpense(this.expenseId);
    }
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
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Unable to load lookup data.';
        this.isLoadingLookups = false;
        this.cdr.detectChanges();
      },
    });
  }

  loadExpense(id: string): void {
    this.isLoadingExpense = true;
    this.errorMessage = '';

    this.expenseService
      .getById(id)
      .pipe(
        take(1),
        finalize(() => {
          this.isLoadingExpense = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (expense) => {
          this.patchExpense(expense);
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to load expense.';
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

          return this.expenseId ? this.expenseService.update(this.expenseId, payload) : this.expenseService.create(payload);
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

  private patchExpense(expense: Expense): void {
    this.form.patchValue({
      description: expense.description,
      date: expense.date?.slice(0, 10) ?? '',
      bankId: expense.bankId,
      price: expense.price,
      sourceId: expense.sourceId,
      tagIds: expense.tags.map((tag) => tag.id),
      manualTags: '',
    });
  }
}
