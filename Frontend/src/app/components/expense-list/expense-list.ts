import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { Expense } from '../../models/api.models';
import { ExpenseService } from '../../services/expense';

@Component({
  selector: 'app-expense-list',
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: './expense-list.html',
  styleUrl: './expense-list.scss',
})
export class ExpenseList implements OnInit {
  private readonly expenseService = inject(ExpenseService);

  readonly displayedColumns = ['item', 'date', 'bank', 'price', 'source', 'tags', 'actions'];

  expenses: Expense[] = [];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.expenseService.getAll().subscribe({
      next: (expenses) => {
        this.expenses = expenses;
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

    this.expenseService.softDelete(id).subscribe({
      next: () => {
        this.expenses = this.expenses.filter((expense) => expense.id !== id);
      },
      error: () => {
        this.errorMessage = 'Unable to delete expense.';
      },
    });
  }
}
