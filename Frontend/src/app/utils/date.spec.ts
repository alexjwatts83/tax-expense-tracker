import { describe, expect, it } from 'vitest';

import {
	CalendarDate,
	isCalendarDate,
	parseCalendarDate,
	toCalendarDate,
	toLocalDateInputValue,
} from './date';

describe('isCalendarDate', () => {
	it('accepts a valid calendar date', () => {
		expect(isCalendarDate('2026-03-10')).toBe(true);
	});

	it('accepts a leap day in a leap year', () => {
		expect(isCalendarDate('2028-02-29')).toBe(true);
	});

	it('rejects a leap day in a non-leap year', () => {
		expect(isCalendarDate('2027-02-29')).toBe(false);
	});

	it('rejects out-of-range months and days', () => {
		expect(isCalendarDate('2026-13-01')).toBe(false);
		expect(isCalendarDate('2026-00-10')).toBe(false);
		expect(isCalendarDate('2026-04-31')).toBe(false);
	});

	it('rejects non-date and mistyped values', () => {
		expect(isCalendarDate('2026-3-10')).toBe(false);
		expect(isCalendarDate('10/03/2026')).toBe(false);
		expect(isCalendarDate('')).toBe(false);
		expect(isCalendarDate(20260310 as unknown)).toBe(false);
	});
});

describe('toCalendarDate', () => {
	it('keeps a valid calendar-date string unchanged', () => {
		expect(toCalendarDate('2026-03-10')).toBe('2026-03-10');
	});

	it('truncates an ISO timestamp to its calendar date without shifting', () => {
		expect(toCalendarDate('2026-03-10T23:59:59.000Z')).toBe('2026-03-10');
	});

	it('formats a Date using local components', () => {
		expect(toCalendarDate(new Date(2026, 2, 10))).toBe('2026-03-10');
	});

	it('returns null for invalid input', () => {
		expect(toCalendarDate('2026-02-30')).toBeNull();
		expect(toCalendarDate('not-a-date')).toBeNull();
		expect(toCalendarDate(null)).toBeNull();
		expect(toCalendarDate(new Date('invalid'))).toBeNull();
	});

	it('round-trips a calendar date regardless of local time', () => {
		const calendarDate = '2026-03-10' as CalendarDate;
		const parsed = parseCalendarDate(calendarDate);

		expect(parsed).not.toBeNull();
		expect(toCalendarDate(parsed)).toBe(calendarDate);
	});
});

describe('parseCalendarDate', () => {
	it('produces a local midnight date matching the calendar day', () => {
		const parsed = parseCalendarDate('2026-03-10');

		expect(parsed?.getFullYear()).toBe(2026);
		expect(parsed?.getMonth()).toBe(2);
		expect(parsed?.getDate()).toBe(10);
		expect(parsed?.getHours()).toBe(0);
	});

	it('does not shift the day across timezone boundaries', () => {
		expect(toLocalDateInputValue(parseCalendarDate('2026-03-10')!)).toBe('2026-03-10');
	});

	it('returns null for invalid input', () => {
		expect(parseCalendarDate('2026-02-30')).toBeNull();
		expect(parseCalendarDate(undefined)).toBeNull();
	});
});
