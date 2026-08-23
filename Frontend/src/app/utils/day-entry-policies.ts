import { AbstractControl, Validators } from '@angular/forms';

import { DayEntryType } from '../models/api.models';

export const DAY_ENTRY_PAGE_SIZES = [10, 20, 50] as const;

export interface PageSlice<T> {
  items: T[];
  totalPages: number;
  rangeLabel: string;
}

export function applySpecificHoursPolicy(
  control: AbstractControl<number | null>,
  entryType: DayEntryType,
): void {
  if (entryType === DayEntryType.SpecificHours) {
    control.setValidators([Validators.required, Validators.min(0.25), Validators.max(24)]);
  } else {
    control.clearValidators();
    control.setValue(null);
  }

  control.updateValueAndValidity();
}

export function getPageSlice<T>(entries: readonly T[], page: number, pageSize: number): PageSlice<T> {
  const normalizedPageSize = Math.max(1, pageSize);
  const totalPages = Math.max(1, Math.ceil(entries.length / normalizedPageSize));
  const normalizedPage = Math.min(Math.max(1, page), totalPages);
  const startIndex = (normalizedPage - 1) * normalizedPageSize;
  const endIndex = Math.min(startIndex + normalizedPageSize, entries.length);

  return {
    items: entries.slice(startIndex, endIndex),
    totalPages,
    rangeLabel: entries.length === 0
      ? '0 of 0'
      : `${startIndex + 1}-${endIndex} of ${entries.length}`,
  };
}
