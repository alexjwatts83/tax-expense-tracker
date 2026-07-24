import { Component, Input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DateInputDirective } from './date-input.directive';

@Component({
  selector: 'app-date-picker-toggle',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  template: `
    <button
      mat-icon-button
      type="button"
      [attr.aria-label]="ariaLabel"
      (click)="openDatePicker()">
      <mat-icon>calendar_month</mat-icon>
    </button>
  `,
})
export class DatePickerToggleComponent {
  @Input({ required: true }) for!: DateInputDirective | HTMLInputElement;
  @Input() ariaLabel = 'Open calendar';

  openDatePicker(): void {
    const inputElement = this.resolveInputElement();
    inputElement.focus();

    const pickerInput = inputElement as HTMLInputElement & { showPicker?: () => void };
    pickerInput.showPicker?.();
  }

  private resolveInputElement(): HTMLInputElement {
    if (this.for instanceof DateInputDirective) {
      return this.for.inputElement;
    }

    return this.for;
  }
}
