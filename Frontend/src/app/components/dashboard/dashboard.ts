import { CommonModule, CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ExpenseSummary } from '../../models/api.models';
import { ExpenseService } from '../../services/expense';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, CurrencyPipe, MatCardModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly expenseService = inject(ExpenseService);

  summary: ExpenseSummary | null = null;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.expenseService.getSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load dashboard summary. Ensure the API is running.';
        this.isLoading = false;
      },
    });
  }

  get topTracker(): string {
    return this.summary?.bySource[0]?.source ?? 'Not available';
  }

  get topBank(): string {
    return this.summary?.byBank[0]?.bank ?? 'Not available';
  }
}
