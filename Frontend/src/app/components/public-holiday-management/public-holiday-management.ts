import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { finalize, take } from 'rxjs';
import { PublicHoliday } from '../../models/api.models';
import { PublicHolidayService } from '../../services/public-holiday';

@Component({
  selector: 'app-public-holiday-management',
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './public-holiday-management.html',
  styleUrl: './public-holiday-management.scss',
})
export class PublicHolidayManagement implements OnInit {
  private readonly publicHolidayService = inject(PublicHolidayService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  readonly displayedColumns: string[] = ['holidayDate', 'name', 'source', 'createdAt'];

  readonly importForm = this.formBuilder.group({
    source: ['Manual Upload'],
  });

  readonly filterForm = this.formBuilder.group({
    fromDate: [''],
    toDate: [''],
  });

  holidays: PublicHoliday[] = [];
  selectedFile: File | null = null;
  isLoading = false;
  isImporting = false;
  errorMessage = '';
  infoMessage = '';

  ngOnInit(): void {
    this.loadHolidays();
  }

  loadHolidays(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const value = this.filterForm.getRawValue();

    this.publicHolidayService
      .getAll({
        fromDate: value.fromDate || undefined,
        toDate: value.toDate || undefined,
      })
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (holidays) => {
          this.holidays = holidays;
        },
        error: () => {
          this.errorMessage = 'Unable to load public holidays. Ensure the API is running.';
        },
      });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  importHolidays(): void {
    if (!this.selectedFile || this.isImporting) {
      this.errorMessage = 'Select a CSV file to import.';
      return;
    }

    this.isImporting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.publicHolidayService
      .import(this.selectedFile, this.importForm.value.source?.trim() || undefined)
      .pipe(
        take(1),
        finalize(() => {
          this.isImporting = false;
        }),
      )
      .subscribe({
        next: (result) => {
          this.infoMessage = `Imported ${result.importedCount} holiday(s).` +
            (result.warnings.length > 0 ? ` ${result.warnings.join(' ')}` : '');
          this.selectedFile = null;
          if (this.fileInput) {
            this.fileInput.nativeElement.value = '';
          }
          this.loadHolidays();
        },
        error: (err) => {
          this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to import public holidays.';
        },
      });
  }

  clearFilters(): void {
    this.filterForm.patchValue({
      fromDate: '',
      toDate: '',
    });
    this.loadHolidays();
  }
}