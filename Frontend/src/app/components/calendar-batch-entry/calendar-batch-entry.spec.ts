import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { CalendarBatchEntry } from './calendar-batch-entry';

describe('CalendarBatchEntry', () => {
  async function createComponent(): Promise<{
    fixture: ComponentFixture<CalendarBatchEntry>;
    http: HttpTestingController;
  }> {
    await TestBed.configureTestingModule({
      imports: [CalendarBatchEntry],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    return {
      fixture: TestBed.createComponent(CalendarBatchEntry),
      http: TestBed.inject(HttpTestingController),
    };
  }

  it('loads and displays complete work weeks around the selected month', async () => {
    const { fixture, http } = await createComponent();
    fixture.componentInstance.monthAnchor = new Date(2026, 8, 1);

    fixture.detectChanges();

    const requests = http.match(
      (request) =>
        request.params.get('fromDate') === '2026-08-31' &&
        request.params.get('toDate') === '2026-10-02',
    );
    expect(requests).toHaveLength(3);
    requests.forEach((request) => request.flush([]));
    await fixture.whenStable();

    const weeks = fixture.componentInstance.weeks;
    expect(weeks).toHaveLength(5);
    expect(weeks[0]?.days.map((day) => day?.dateIso)).toEqual([
      '2026-08-31',
      '2026-09-01',
      '2026-09-02',
      '2026-09-03',
      '2026-09-04',
    ]);
    expect(weeks.at(-1)?.days.map((day) => day?.dateIso)).toEqual([
      '2026-09-28',
      '2026-09-29',
      '2026-09-30',
      '2026-10-01',
      '2026-10-02',
    ]);
    expect(weeks.every((week) => week.days.every((day) => day !== null))).toBe(true);
    http.verify();
  });
});