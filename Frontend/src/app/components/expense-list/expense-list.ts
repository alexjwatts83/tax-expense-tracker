import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { Expense, Tag, Tracker } from '../../models/api.models';
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
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: './expense-list.html',
  styleUrl: './expense-list.scss',
})
export class ExpenseList implements OnInit {
  private readonly expenseService = inject(ExpenseService);
  private readonly trackerService = inject(TrackerService);
  private readonly tagService = inject(TagService);
  private readonly formBuilder = inject(FormBuilder);

  readonly displayedColumns = ['item', 'date', 'bank', 'price', 'source', 'tags', 'actions'];
  readonly pageSizes = [10, 20, 50];

  readonly filterForm = this.formBuilder.group({
    startDate: [''],
    endDate: [''],
    bank: [''],
    minPrice: [''],
    maxPrice: [''],
    sourceId: [''],
    tagIds: [[] as string[]],
  });

  expenses: Expense[] = [];
  pagedExpenses: Expense[] = [];
  lastDeletedExpense: Expense | null = null;
  trackers: Tracker[] = [];
  tags: Tag[] = [];

  page = 1;
  pageSize = 20;
  hasActiveFilters = false;

  isLoading = false;
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.loadLookups();
    this.loadExpenses();
  }

  loadLookups(): void {
    this.trackerService.getAll().subscribe({
      next: (trackers) => {
        this.trackers = trackers;
      },
    });

    this.tagService.getAll().subscribe({
      next: (tags) => {
        this.tags = tags;
      },
    });
  }

  loadExpenses(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.expenseService.getAll().subscribe({
      next: (expenses) => {
        this.expenses = expenses;
        this.updatePagedExpenses();
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load expenses. Ensure the API is running.';
        this.isLoading = false;
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
        this.expenses = this.expenses.filter((expense) => expense.id !== id);
        this.updatePagedExpenses();
        this.infoMessage = `Expense "${expense.item}" deleted.`;
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
        this.expenses = [...this.expenses, expense].sort((a, b) =>
          new Date(b.date).getTime() - new Date(a.date).getTime(),
        );
        this.lastDeletedExpense = null;
        this.updatePagedExpenses();
        this.infoMessage = `Expense "${expense.item}" restored.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to restore expense.';
      },
    });
  }

  applyFilters(): void {
    const value = this.filterForm.value;

    const request = {
      startDate: value.startDate || undefined,
      endDate: value.endDate || undefined,
      bank: value.bank?.trim() || undefined,
      minPrice: value.minPrice ? Number(value.minPrice) : undefined,
      maxPrice: value.maxPrice ? Number(value.maxPrice) : undefined,
      sourceId: value.sourceId || undefined,
      tagIds: value.tagIds && value.tagIds.length > 0 ? value.tagIds : undefined,
    };

    this.hasActiveFilters = Object.values(request).some((v) => v !== undefined);
    this.page = 1;
    this.isLoading = true;
    this.errorMessage = '';

    this.expenseService.filter(request).subscribe({
      next: (expenses) => {
        this.expenses = expenses;
        this.updatePagedExpenses();
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? 'Unable to apply filters.';
        this.isLoading = false;
      },
    });
  }

  clearFilters(): void {
    this.filterForm.reset({
      startDate: '',
      endDate: '',
      bank: '',
      minPrice: '',
      maxPrice: '',
      sourceId: '',
      tagIds: [],
    });

    this.hasActiveFilters = false;
    this.page = 1;
    this.lastDeletedExpense = null;
    this.loadExpenses();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page -= 1;
      this.updatePagedExpenses();
    }
  }

  nextPage(): void {
    if (this.page * this.pageSize < this.expenses.length) {
      this.page += 1;
      this.updatePagedExpenses();
    }
  }

  onPageSizeChange(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    this.updatePagedExpenses();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.expenses.length / this.pageSize));
  }

  private updatePagedExpenses(): void {
    const start = (this.page - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.pagedExpenses = this.expenses.slice(start, end);
  }
}
