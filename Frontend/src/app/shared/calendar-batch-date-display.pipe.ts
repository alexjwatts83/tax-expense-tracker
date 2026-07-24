import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'calendarBatchDateDisplay',
  standalone: true,
})
export class CalendarBatchDateDisplayPipe implements PipeTransform {
  transform(value: Date | string | null | undefined): string {
    const date = this.toDate(value);
    if (!date) {
      return '';
    }

    const weekday = new Intl.DateTimeFormat('en-US', { weekday: 'short' }).format(date);
    const day = new Intl.DateTimeFormat('en-US', { day: '2-digit' }).format(date);
    const month = new Intl.DateTimeFormat('en-US', { month: 'long' }).format(date);

    return `${weekday}, ${day} ${month}`;
  }

  private toDate(value: Date | string | null | undefined): Date | null {
    if (!value) {
      return null;
    }

    if (value instanceof Date) {
      return Number.isNaN(value.getTime()) ? null : value;
    }

    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }

    const datePart = trimmed.length >= 10 ? trimmed.slice(0, 10) : trimmed;
    const parts = datePart.split('-').map(Number);
    if (parts.length === 3 && parts.every((part) => Number.isFinite(part))) {
      const [year, month, day] = parts;
      const date = new Date(year, (month ?? 1) - 1, day ?? 1);
      return Number.isNaN(date.getTime()) ? null : date;
    }

    const fallback = new Date(trimmed);
    return Number.isNaN(fallback.getTime()) ? null : fallback;
  }
}