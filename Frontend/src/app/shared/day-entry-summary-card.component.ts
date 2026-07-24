import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { DayEntrySummary } from '../models/api.models';
import { StandardDateDisplayPipe } from './standard-date-display.pipe';
import { DateInputDirective } from './date-input.directive';
import { DatePickerToggleComponent } from './date-picker-toggle.component';

@Component({
  selector: 'app-day-entry-summary-card',
  standalone: true,
  imports: [
    CommonModule,
    StandardDateDisplayPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    DateInputDirective,
    DatePickerToggleComponent,
  ],
  templateUrl: './day-entry-summary-card.component.html',
  styleUrl: './day-entry-summary-card.component.scss',
})
export class DayEntrySummaryCardComponent {
  @Input({ required: true }) summaryForm!: FormGroup;
  @Input({ required: true }) summaryViewOptions!: ReadonlyArray<{ value: 'week' | 'month'; label: string }>;
  @Input() isLoadingSummary = false;
  @Input() summary: DayEntrySummary | null = null;
  @Input() datePickerAriaLabel = 'Open summary date calendar';
  @Input() noSummaryMessage = 'Select a period to load summary details.';
  @Input() noHolidaysMessage = 'No holiday markers in this period.';
  @Output() refresh = new EventEmitter<void>();
}
