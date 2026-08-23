// Calendar dates are timezone-free 'yyyy-MM-dd' values; timestamps are UTC instants.
export type CalendarDate = string & { readonly __brand: 'CalendarDate' };

const CALENDAR_DATE_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;

export function toLocalDateInputValue(date: Date = new Date()): string {
	const year = date.getFullYear();
	const month = `${date.getMonth() + 1}`.padStart(2, '0');
	const day = `${date.getDate()}`.padStart(2, '0');

	return `${year}-${month}-${day}`;
}

export function isCalendarDate(value: unknown): value is CalendarDate {
	if (typeof value !== 'string')
		return false;

	const match = CALENDAR_DATE_PATTERN.exec(value);
	if (!match)
		return false;

	const [, year, month, day] = match.map(Number);
	if (month < 1 || month > 12 || day < 1 || day > daysInMonth(year, month))
		return false;

	return true;
}

// Converts a Date or string to a strict 'yyyy-MM-dd' using local calendar components; never applies timezone conversion.
export function toCalendarDate(value: Date | string | null | undefined): CalendarDate | null {
	if (value instanceof Date)
		return Number.isNaN(value.getTime()) ? null : (toLocalDateInputValue(value) as CalendarDate);

	if (typeof value !== 'string')
		return null;

	const trimmed = value.trim();
	if (!trimmed)
		return null;

	const candidate = trimmed.length >= 10 ? trimmed.slice(0, 10) : trimmed;
	return isCalendarDate(candidate) ? (candidate as CalendarDate) : null;
}

// Parses a calendar date into a local Date at midnight for display; returns null for invalid input.
export function parseCalendarDate(value: Date | string | null | undefined): Date | null {
	if (value instanceof Date)
		return Number.isNaN(value.getTime()) ? null : value;

	const calendarDate = toCalendarDate(value);
	if (!calendarDate)
		return null;

	const [year, month, day] = calendarDate.split('-').map(Number);
	const date = new Date(year, month - 1, day);
	return Number.isNaN(date.getTime()) ? null : date;
}

function daysInMonth(year: number, month: number): number {
	return new Date(year, month, 0).getDate();
}