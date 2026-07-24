import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { finalize, forkJoin, of, switchMap, take, timeout } from 'rxjs';
import { Bank, Expense, Tag, Tracker } from '../../models/api.models';
import { BankService } from '../../services/bank';
import { ExpenseService } from '../../services/expense';
import { TagService } from '../../services/tag';
import { TrackerService } from '../../services/tracker';

@Component({
  selector: 'app-expense-list',
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './expense-list.html',
  styleUrl: './expense-list.scss',
})
export class ExpenseList implements OnInit {
  private readonly bankService = inject(BankService);
  private readonly expenseService = inject(ExpenseService);
  private readonly trackerService = inject(TrackerService);
  private readonly tagService = inject(TagService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns = ['date', 'bank', 'price', 'tracker', 'tags', 'actions'];
  readonly pageSizes = [10, 20, 50];

  readonly filterForm = this.formBuilder.group({
    date: [''],
    bankId: [''],
    price: [''],
    sourceId: [''],
    tagIds: [[] as string[]],
  });

  readonly createForm = this.formBuilder.group({
    description: [''],
    date: ['', [Validators.required]],
    bankId: ['', [Validators.required]],
    price: [0, [Validators.required, Validators.min(0)]],
    sourceId: ['', [Validators.required]],
    tagIds: [[] as string[]],
    manualTags: [''],
  });

  banks: Bank[] = [];
  expenses: Expense[] = [];
  pagedExpenses: Expense[] = [];
  totalCount = 0;
  lastDeletedExpense: Expense | null = null;
  trackers: Tracker[] = [];
  tags: Tag[] = [];

  page = 1;
  pageSize = 20;
  hasActiveFilters = false;

  isLoading = false;
  isFiltering = false;
  isCreating = false;
  errorMessage = '';
  infoMessage = '';
  private inFlightFilterSignature: string | null = null;
  private lastAppliedFilterSignature = '';
  private filterRequestVersion = 0;

  ngOnInit(): void {
    this.loadLookups();
    this.loadExpenses();
  }

  loadLookups(): void {
    forkJoin({
      banks: this.bankService.getAll(),
      trackers: this.trackerService.getAll(),
      tags: this.tagService.getAll(),
    }).subscribe({
      next: ({ banks, trackers, tags }) => {
        this.banks = banks;
        this.trackers = trackers;
        this.tags = tags;
      },
    });
  }

  loadExpenses(): void {
    // Invalidate any in-flight filter request so stale responses are ignored.
    this.filterRequestVersion += 1;
    this.inFlightFilterSignature = null;

    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.expenseService
      .getAll(this.page, this.pageSize)
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (result) => {
          this.expenses = result.items;
          this.pagedExpenses = result.items;
          this.totalCount = result.totalCount;
          this.page = result.pageNumber;
          this.pageSize = result.pageSize;
        },
        error: () => {
          this.errorMessage = 'Unable to load expenses. Ensure the API is running.';
        },
      });
  }

  softDeleteExpense(id: string): void {
    this.errorMessage = '';
    this.infoMessage = '';

    const expense = this.expenses.find((x) => x.id === id);
    if (!expense) {
      return;
    }

    this.expenseService.softDelete(id).subscribe({
      next: () => {
        this.lastDeletedExpense = expense;
        if (this.hasActiveFilters) {
          this.expenses = this.expenses.filter((entry) => entry.id !== id);
          this.totalCount = this.expenses.length;
          this.updatePagedExpenses();
        } else {
          this.loadExpenses();
        }
        this.infoMessage = 'Expense deleted.';
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to delete expense.';
      },
    });
  }

  undoDeleteExpense(): void {
    if (!this.lastDeletedExpense) {
      return;
    }

    const expense = this.lastDeletedExpense;
    this.errorMessage = '';
    this.infoMessage = '';

    this.expenseService.restore(expense.id).subscribe({
      next: () => {
        this.lastDeletedExpense = null;

        if (this.hasActiveFilters) {
          this.applyFilters();
        } else {
          this.loadExpenses();
        }

        this.infoMessage = 'Expense restored.';
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to restore expense.';
      },
    });
  }

  applyFilters(): void {
    const value = this.filterForm.value;

    const request = {
      date: value.date || undefined,
      bankId: value.bankId || undefined,
      price: value.price ? Number(value.price) : undefined,
      sourceId: value.sourceId || undefined,
      tagIds: value.tagIds && value.tagIds.length > 0 ? value.tagIds : undefined,
    };

    const signature = JSON.stringify(request);
    if (signature === this.inFlightFilterSignature || signature === this.lastAppliedFilterSignature) {
      return;
    }

    this.inFlightFilterSignature = signature;
    const requestVersion = ++this.filterRequestVersion;

    this.hasActiveFilters = Object.values(request).some((v) => v !== undefined);

    if (!this.hasActiveFilters) {
      this.lastAppliedFilterSignature = '';
      this.loadExpenses();
      return;
    }

    this.page = 1;
    this.isFiltering = true;
    this.errorMessage = '';

    this.expenseService
      .filter(request)
      .pipe(
        take(1),
        timeout(15000),
        finalize(() => {
          this.isFiltering = false;
          this.inFlightFilterSignature = null;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (expenses) => {
          if (requestVersion !== this.filterRequestVersion) {
            return;
          }

          this.lastAppliedFilterSignature = signature;
          this.expenses = expenses;
          this.totalCount = expenses.length;
          this.updatePagedExpenses();
        },
        error: (err) => {
          this.errorMessage =
            err?.name === 'TimeoutError'
              ? 'Filtering timed out. Please try again.'
              : err?.error?.detail ?? 'Unable to apply filters.';
        },
      });
  }

  createExpenseInline(): void {
    const value = this.createForm.value;

    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      this.errorMessage = 'Please fix the highlighted fields before adding an expense.';
      return;
    }

    if (this.isCreating) {
      return;
    }

    this.isCreating = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.resolveInlineTagIds((value.tagIds ?? []) as string[], this.parseManualTags(value.manualTags))
      .pipe(
        switchMap((tagIds) =>
          this.expenseService.create({
            description: value.description?.trim() ?? '',
            date: value.date ?? '',
            bankId: value.bankId ?? '',
            price: Number(value.price),
            sourceId: value.sourceId ?? '',
            tagIds,
          }),
        ),
        finalize(() => {
          this.isCreating = false;
        }),
      )
      .subscribe({
        next: (createdExpense) => {
          if (this.hasActiveFilters) {
            this.applyFilters();
          } else {
            this.page = 1;
            this.loadExpenses();
          }

          this.createForm.reset({
            description: '',
            date: '',
            bankId: '',
            price: 0,
            sourceId: '',
            tagIds: [],
            manualTags: '',
          });
          this.infoMessage = 'Expense created.';
        },
        error: (err) => {
          this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to create expense.';
        },
      });
  }

  applyInlineTags(): void {
    this.errorMessage = '';

    const value = this.createForm.value;
    this.resolveInlineTagIds((value.tagIds ?? []) as string[], this.parseManualTags(value.manualTags)).subscribe({
      next: (tagIds) => {
        this.createForm.patchValue({ tagIds, manualTags: '' });
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to apply tags.';
      },
    });
  }

  clearFilters(): void {
    this.filterForm.reset({
      date: '',
      bankId: '',
      price: '',
      sourceId: '',
      tagIds: [],
    });

    this.inFlightFilterSignature = null;
    this.lastAppliedFilterSignature = '';
    // Invalidate in-flight filter responses before loading the full table.
    this.filterRequestVersion += 1;
    this.hasActiveFilters = false;
    this.page = 1;
    this.lastDeletedExpense = null;
    this.loadExpenses();
  }

  openDatePicker(input: HTMLInputElement): void {
    input.focus();

    const pickerInput = input as HTMLInputElement & { showPicker?: () => void };
    pickerInput.showPicker?.();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page -= 1;
      if (this.hasActiveFilters) {
        this.updatePagedExpenses();
      } else {
        this.loadExpenses();
      }
    }
  }

  nextPage(): void {
    if (this.page * this.pageSize < this.totalCount) {
      this.page += 1;
      if (this.hasActiveFilters) {
        this.updatePagedExpenses();
      } else {
        this.loadExpenses();
      }
    }
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    if (this.hasActiveFilters) {
      this.updatePagedExpenses();
    } else {
      this.loadExpenses();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  private updatePagedExpenses(): void {
    const start = (this.page - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.pagedExpenses = this.expenses.slice(start, end);
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

  private resolveInlineTagIds(selectedTagIds: string[], manualTagNames: string[]) {
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
}
