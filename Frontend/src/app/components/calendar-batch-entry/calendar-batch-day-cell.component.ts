import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { DayEntryType } from '../../models/api.models';

type DayCategory = 'none' | 'wfh' | 'office' | 'annual' | 'sick';

interface CalendarDayRowVm {
  dateIso: string;
  dayLabel: string;
  isToday: boolean;
  category: DayCategory;
  originalCategory: DayCategory;
  entryType: DayEntryType;
  originalEntryType: DayEntryType;
  specificHours: number | null;
  originalSpecificHours: number | null;
  leaveType: number;
  originalLeaveType: number;
  leaveId: string | null;
  workLocationId: string | null;
  isHoliday: boolean;
  holidayName: string;
  canBeWorkedOnHoliday: boolean;
  resultState: 'applied' | 'failed' | 'skipped' | null;
  resultMessage: string;
}

@Component({
  selector: 'app-calendar-batch-day-cell',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatIconModule, MatInputModule, MatRadioModule, MatSelectModule],
  templateUrl: './calendar-batch-day-cell.component.html',
  styleUrl: './calendar-batch-day-cell.component.scss',
})
export class CalendarBatchDayCellComponent {
  @Input({ required: true }) dayCell!: CalendarDayRowVm;
  @Input({ required: true }) categoryOptions!: ReadonlyArray<{ value: DayCategory; label: string }>;
  @Input({ required: true }) entryTypeOptions!: ReadonlyArray<{ value: DayEntryType; label: string }>;
  @Input() isHolidayLocked = false;
  @Input() isSpecificHoursRow = false;
  @Input() isPendingRow = false;
  @Input() rowHasHoursError = false;
  @Input() showWeekWfhAction = false;

  @Output() categoryChange = new EventEmitter<DayCategory>();
  @Output() entryTypeChange = new EventEmitter<DayEntryType>();
  @Output() weekWfhClick = new EventEmitter<void>();
}
