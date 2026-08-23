import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';

import { DayEntryType } from '../models/api.models';
import { applySpecificHoursPolicy, getPageSlice } from './day-entry-policies';

describe('applySpecificHoursPolicy', () => {
  it('requires specific hours within the supported range', () => {
    const control = new FormControl<number | null>(null);

    applySpecificHoursPolicy(control, DayEntryType.SpecificHours);

    expect(control.hasError('required')).toBe(true);
    control.setValue(0.1);
    expect(control.hasError('min')).toBe(true);
    control.setValue(25);
    expect(control.hasError('max')).toBe(true);
    control.setValue(7.6);
    expect(control.valid).toBe(true);
  });

  it('clears validators and irrelevant values for non-specific entries', () => {
    const control = new FormControl<number | null>(4);
    applySpecificHoursPolicy(control, DayEntryType.SpecificHours);

    applySpecificHoursPolicy(control, DayEntryType.FullDay);

    expect(control.value).toBeNull();
    expect(control.valid).toBe(true);
  });
});

describe('getPageSlice', () => {
  it('returns the requested page and range label', () => {
    const result = getPageSlice(Array.from({ length: 23 }, (_, index) => index + 1), 2, 10);

    expect(result.items).toEqual([11, 12, 13, 14, 15, 16, 17, 18, 19, 20]);
    expect(result.totalPages).toBe(3);
    expect(result.rangeLabel).toBe('11-20 of 23');
  });

  it('clamps pages and handles empty collections', () => {
    expect(getPageSlice([1, 2, 3], 99, 2)).toEqual({
      items: [3],
      totalPages: 2,
      rangeLabel: '3-3 of 3',
    });
    expect(getPageSlice([], 1, 10)).toEqual({
      items: [],
      totalPages: 1,
      rangeLabel: '0 of 0',
    });
  });
});
