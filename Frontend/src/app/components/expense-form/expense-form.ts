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
import { finalize, forkJoin, of, switchMap, take, tap } from 'rxjs';
import { Bank, Expense, Tag, Tracker } from '../../models/api.models';
import { BankService } from '../../services/bank';
import { ExpenseService } from '../../services/expense';
import { TagService } from '../../services/tag';
import { TrackerService } from '../../services/tracker';
import { DateInputDirective } from '../../shared/date-input.directive';
import { DatePickerToggleComponent } from '../../shared/date-picker-toggle.component';
import { parseApiError } from '../../utils/api-problem';
import { parseManualTagNames, pruneStaleTagIds, resolveTagIds, TagResolution } from '../../utils/tag-resolution';

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
        error: (error) => void this.showApiError(error, 'Unable to load expense.'),
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
        switchMap((resolution) => {
          if (resolution.failedNames.length > 0) {
            this.handleFailedTagCreation(resolution);
            this.isSubmitting = false;
            return of(null);
          }

          const payload = {
            description: this.form.value.description?.trim() ?? '',
            date: this.form.value.date,
            bankId: this.form.value.bankId,
            price: Number(this.form.value.price),
            sourceId: this.form.value.sourceId,
            tagIds: resolution.tagIds,
          };

          return this.expenseId ? this.expenseService.update(this.expenseId, payload) : this.expenseService.create(payload);
        }),
      )
      .subscribe({
        next: (result) => {
          if (result === null) {
            return;
          }

          this.isSubmitting = false;
          this.router.navigate(['/expenses']);
        },
        error: (error) => {
          void this.handleSaveError(error);
        },
      });
  }

  applyManualTags(): void {
    this.errorMessage = '';

    this.applyManualTagsInternal().subscribe({
      next: (resolution) => {
        this.form.patchValue({ tagIds: resolution.tagIds, manualTags: resolution.failedNames.join(', ') });
        if (resolution.failedNames.length > 0) {
          this.handleFailedTagCreation(resolution);
        }
      },
      error: (error) => void this.showApiError(error, 'Unable to apply manual tags.'),
    });
  }

  private handleFailedTagCreation(resolution: TagResolution): void {
    this.errorMessage = `Some tags could not be created and remain for retry: ${resolution.failedNames.join(', ')}.`;
    this.cdr.detectChanges();
  }

  private async handleSaveError(error: unknown): Promise<void> {
    this.isSubmitting = false;
    const problem = await parseApiError(error, 'Unable to save expense.');

    if (problem.classification === 'invalid-reference') {
      const { keptIds, removedIds } = pruneStaleTagIds((this.form.value.tagIds ?? []) as string[], this.tags);
      if (removedIds.length > 0) {
        this.form.patchValue({ tagIds: keptIds });
        this.errorMessage = 'Some selected tags no longer exist and were removed. Review and save again.';
        this.cdr.detectChanges();
        return;
      }
    }

    this.errorMessage = problem.message;
    this.cdr.detectChanges();
  }

  private async showApiError(error: unknown, fallbackMessage: string): Promise<void> {
    this.errorMessage = (await parseApiError(error, fallbackMessage)).message;
    this.cdr.detectChanges();
  }

  private parseManualTags(rawValue: string | null | undefined): string[] {
    return parseManualTagNames(rawValue);
  }

  private applyManualTagsInternal() {
    const selectedTagIds = (this.form.value.tagIds ?? []) as string[];
    const manualTagNames = this.parseManualTags(this.form.value.manualTags);
    return resolveTagIds(selectedTagIds, manualTagNames, this.tags, (name) => this.tagService.create({ name })).pipe(
      tap((resolution) => {
        if (resolution.createdTags.length > 0) {
          this.tags = [...this.tags, ...resolution.createdTags].sort((a, b) => a.name.localeCompare(b.name));
        }
      }),
    );
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
