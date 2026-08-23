import { describe, expect, it } from 'vitest';

import {
  DayEntryType,
  LeaveBatchCreateResult,
  LeaveType,
  WorkLocationBatchCreateResult,
  WorkLocationType,
} from '../models/api.models';
import {
  CalendarChangeExecution,
  CalendarChangeRow,
  planCalendarChanges,
  reconcileCalendarChanges,
} from './calendar-change-set';

function row(overrides: Partial<CalendarChangeRow> = {}): CalendarChangeRow {
  return {
    dateIso: '2026-08-03',
    category: 'none',
    originalCategory: 'none',
    entryType: DayEntryType.FullDay,
    originalEntryType: DayEntryType.FullDay,
    specificHours: null,
    originalSpecificHours: null,
    leaveType: LeaveType.Annual,
    originalLeaveType: LeaveType.Annual,
    leaveId: null,
    workLocationId: null,
    ...overrides,
  };
}

function emptyWorkResult(): WorkLocationBatchCreateResult {
  return { totalRequested: 0, createdCount: 0, skippedCount: 0, failedCount: 0, results: [] };
}

function emptyLeaveResult(): LeaveBatchCreateResult {
  return { totalRequested: 0, createdCount: 0, skippedCount: 0, failedCount: 0, results: [] };
}

function execution(overrides: Partial<CalendarChangeExecution> = {}): CalendarChangeExecution {
  return {
    workCreates: emptyWorkResult(),
    leaveCreates: emptyLeaveResult(),
    workDeletes: [],
    leaveDeletes: [],
    workUpdates: [],
    leaveUpdates: [],
    ...overrides,
  };
}

describe('planCalendarChanges', () => {
  it('plans create, update, and delete operations with normalized payloads', () => {
    const plan = planCalendarChanges([
      row({
        dateIso: '2026-08-03',
        category: 'office',
        entryType: DayEntryType.SpecificHours,
        specificHours: 4,
      }),
      row({
        dateIso: '2026-08-04',
        category: 'annual',
        originalCategory: 'wfh',
        workLocationId: 'work-delete',
      }),
      row({
        dateIso: '2026-08-05',
        category: 'sick',
        originalCategory: 'annual',
        leaveType: LeaveType.Sick,
        originalLeaveType: LeaveType.Annual,
        leaveId: 'leave-update',
      }),
      row({
        dateIso: '2026-08-06',
        category: 'wfh',
        originalCategory: 'office',
        workLocationId: 'work-update',
      }),
      row({
        dateIso: '2026-08-07',
        category: 'office',
        originalCategory: 'annual',
        leaveId: 'leave-delete',
      }),
      row({ dateIso: '2026-08-10' }),
    ]);

    expect(plan.changedDateIsos).toHaveLength(5);
    expect(plan.workCreates).toHaveLength(2);
    expect(plan.workCreates[0].payload).toMatchObject({
      workDate: '2026-08-03',
      workLocation: WorkLocationType.Office,
      entryType: DayEntryType.SpecificHours,
      specificHours: 4,
      notes: null,
    });
    expect(plan.leaveCreates.map((operation) => operation.dateIso)).toEqual(['2026-08-04']);
    expect(plan.workDeletes.map((operation) => operation.id)).toEqual(['work-delete']);
    expect(plan.leaveDeletes.map((operation) => operation.id)).toEqual(['leave-delete']);
    expect(plan.workUpdates.map((operation) => operation.id)).toEqual(['work-update']);
    expect(plan.leaveUpdates.map((operation) => operation.id)).toEqual(['leave-update']);
  });

  it('uses existing target IDs to plan only the unfinished half of a transition', () => {
    const workDeleteOnly = planCalendarChanges([
      row({
        category: 'annual',
        originalCategory: 'wfh',
        workLocationId: 'old-work',
        leaveId: 'created-leave',
      }),
    ]);
    const leaveCreateOnly = planCalendarChanges([
      row({
        category: 'annual',
        originalCategory: 'wfh',
        workLocationId: null,
        leaveId: null,
      }),
    ]);

    expect(workDeleteOnly.leaveCreates).toEqual([]);
    expect(workDeleteOnly.workDeletes).toHaveLength(1);
    expect(leaveCreateOnly.leaveCreates).toHaveLength(1);
    expect(leaveCreateOnly.workDeletes).toEqual([]);
  });
});

describe('reconcileCalendarChanges', () => {
  it('reconciles created and skipped rows without mutating inputs', () => {
    const rows = [
      row({ dateIso: '2026-08-03', category: 'office' }),
      row({ dateIso: '2026-08-04', category: 'annual' }),
    ];
    const plan = planCalendarChanges(rows);

    const result = reconcileCalendarChanges(rows, plan, execution({
      workCreates: {
        totalRequested: 1,
        createdCount: 1,
        skippedCount: 0,
        failedCount: 0,
        results: [{
          workDate: '2026-08-03T00:00:00',
          workLocation: WorkLocationType.Office,
          entryType: DayEntryType.FullDay,
          status: 'Created',
          entry: {
            id: 'new-work',
            workDate: '2026-08-03',
            workLocation: WorkLocationType.Office,
            entryType: DayEntryType.FullDay,
            hoursWorked: 7.6,
            createdAt: '2026-08-01T00:00:00Z',
            updatedAt: '2026-08-01T00:00:00Z',
          },
        }],
      },
      leaveCreates: {
        totalRequested: 1,
        createdCount: 0,
        skippedCount: 1,
        failedCount: 0,
        results: [{
          leaveDate: '2026-08-04',
          leaveType: LeaveType.Annual,
          entryType: DayEntryType.FullDay,
          status: 'SkippedDuplicate',
          message: 'Already exists.',
        }],
      },
    }));

    expect(result).toMatchObject({ added: 1, updated: 0, skipped: 1, failed: 0 });
    expect(result.rows).toEqual([
      expect.objectContaining({ dateIso: '2026-08-03', state: 'applied', syncOriginal: true, workLocationId: 'new-work' }),
      expect.objectContaining({ dateIso: '2026-08-04', state: 'skipped', syncOriginal: false, message: 'Already exists.' }),
    ]);
    expect(rows[0].workLocationId).toBeNull();
  });

  it('turns a request-level create failure into a retryable row failure', () => {
    const rows = [row({ category: 'wfh' })];
    const plan = planCalendarChanges(rows);

    const result = reconcileCalendarChanges(rows, plan, execution({ workCreateError: 'Request unavailable.' }));

    expect(result).toMatchObject({ added: 0, updated: 0, skipped: 0, failed: 1 });
    expect(result.rows[0]).toMatchObject({ state: 'failed', message: 'Request unavailable.', syncOriginal: false });
  });

  it('keeps a transition pending when create succeeds but delete fails', () => {
    const rows = [row({
      category: 'annual',
      originalCategory: 'wfh',
      workLocationId: 'old-work',
    })];
    const plan = planCalendarChanges(rows);

    const result = reconcileCalendarChanges(rows, plan, execution({
      leaveCreates: {
        totalRequested: 1,
        createdCount: 1,
        skippedCount: 0,
        failedCount: 0,
        results: [{
          leaveDate: '2026-08-03',
          leaveType: LeaveType.Annual,
          entryType: DayEntryType.FullDay,
          status: 'Created',
          entry: {
            id: 'new-leave',
            leaveDate: '2026-08-03',
            leaveType: LeaveType.Annual,
            entryType: DayEntryType.FullDay,
            hoursWorked: 7.6,
            createdAt: '2026-08-01T00:00:00Z',
            updatedAt: '2026-08-01T00:00:00Z',
          },
        }],
      },
      workDeletes: [{ dateIso: '2026-08-03', ok: false, message: 'Delete failed.' }],
    }));

    expect(result.rows[0]).toMatchObject({
      state: 'failed',
      message: 'Delete failed.',
      syncOriginal: false,
      leaveId: 'new-leave',
    });
  });

  it('keeps a transition pending when delete succeeds but create fails', () => {
    const rows = [row({
      category: 'annual',
      originalCategory: 'wfh',
      workLocationId: 'old-work',
    })];
    const plan = planCalendarChanges(rows);

    const result = reconcileCalendarChanges(rows, plan, execution({
      leaveCreateError: 'Create failed.',
      workDeletes: [{ dateIso: '2026-08-03', ok: true, message: '' }],
    }));

    expect(result.rows[0]).toMatchObject({
      state: 'failed',
      message: 'Create failed.',
      syncOriginal: false,
      workLocationId: null,
    });
  });
});
