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
import { Bank } from '../../models/api.models';
import { BankService } from '../../services/bank';

@Component({
  selector: 'app-bank-management',
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
  templateUrl: './bank-management.html',
  styleUrl: './bank-management.scss',
})
export class BankManagement implements OnInit {
  private readonly bankService = inject(BankService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns: string[] = ['name', 'createdAt', 'actions'];

  readonly bankForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
  });

  banks: Bank[] = [];
  lastDeletedBank: Bank | null = null;
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.loadBanks();
  }

  loadBanks(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.bankService
      .getAll()
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (banks) => {
          this.banks = banks;
        },
        error: () => {
          this.errorMessage = 'Unable to load banks. Ensure the API is running.';
        },
      });
  }

  createBank(): void {
    if (this.bankForm.invalid || this.isSubmitting) {
      this.bankForm.markAllAsTouched();
      return;
    }

    const name = this.bankForm.value.name?.trim() ?? '';

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.bankService.create({ name }).subscribe({
      next: (created) => {
        this.banks = [created, ...this.banks];
        this.bankForm.reset();
        this.lastDeletedBank = null;
        this.infoMessage = 'Bank created.';
        this.isSubmitting = false;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to create bank.';
        this.isSubmitting = false;
      },
    });
  }

  softDeleteBank(id: string): void {
    this.errorMessage = '';
    this.infoMessage = '';

    const bank = this.banks.find((x) => x.id === id);
    if (!bank) {
      return;
    }

    this.bankService.softDelete(id).subscribe({
      next: () => {
        this.lastDeletedBank = bank;
        this.banks = this.banks.filter((entry) => entry.id !== id);
        this.infoMessage = `Bank "${bank.name}" deleted.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to delete bank.';
      },
    });
  }

  undoDeleteBank(): void {
    if (!this.lastDeletedBank) {
      return;
    }

    const bank = this.lastDeletedBank;
    this.errorMessage = '';
    this.infoMessage = '';

    this.bankService.restore(bank.id).subscribe({
      next: () => {
        this.banks = [bank, ...this.banks].sort((a, b) => a.name.localeCompare(b.name));
        this.lastDeletedBank = null;
        this.infoMessage = `Bank "${bank.name}" restored.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to restore bank.';
      },
    });
  }
}
