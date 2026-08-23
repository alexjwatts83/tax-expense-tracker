import {
  CreateLeaveRequest,
  CreateWorkLocationRequest,
  DayEntryType,
  LeaveBatchCreateResult,
  LeaveType,
  WorkLocationBatchCreateResult,
  WorkLocationType,
} from '../models/api.models';

export type CalendarDayCategory = 'none' | 'wfh' | 'office' | 'annual' | 'sick';

export interface CalendarChangeRow {
  dateIso: string;
  category: CalendarDayCategory;
  originalCategory: CalendarDayCategory;
  entryType: DayEntryType;
  originalEntryType: DayEntryType;
  specificHours: number | null;
  originalSpecificHours: number | null;
  leaveType: LeaveType;
  originalLeaveType: LeaveType;
  leaveId: string | null;
  workLocationId: string | null;
}

export interface CalendarCreateOperation<TPayload> {
  dateIso: string;
  payload: TPayload;
}

export interface CalendarPersistedOperation<TPayload = never> {
  dateIso: string;
  id: string;
  payload?: TPayload;
}

export interface CalendarChangeSet {
  changedDateIsos: string[];
  workCreates: Array<CalendarCreateOperation<CreateWorkLocationRequest>>;
  leaveCreates: Array<CalendarCreateOperation<CreateLeaveRequest>>;
  workDeletes: Array<CalendarPersistedOperation>;
  leaveDeletes: Array<CalendarPersistedOperation>;
  workUpdates: Array<CalendarPersistedOperation<CreateWorkLocationRequest>>;
  leaveUpdates: Array<CalendarPersistedOperation<CreateLeaveRequest>>;
}

export interface CalendarOperationResult {
  dateIso: string;
  ok: boolean;
  message: string;
}

export interface CalendarChangeExecution {
  workCreates: WorkLocationBatchCreateResult;
  leaveCreates: LeaveBatchCreateResult;
  workDeletes: CalendarOperationResult[];
  leaveDeletes: CalendarOperationResult[];
  workUpdates: CalendarOperationResult[];
  leaveUpdates: CalendarOperationResult[];
  workCreateError?: string;
  leaveCreateError?: string;
}

export interface CalendarRowReconciliation {
  dateIso: string;
  state: 'applied' | 'failed' | 'skipped';
  message: string;
  syncOriginal: boolean;
  workLocationId?: string | null;
  leaveId?: string | null;
}

export interface CalendarReconciliation {
  rows: CalendarRowReconciliation[];
  added: number;
  updated: number;
  skipped: number;
  failed: number;
}

interface MutableRowOutcome {
  row: CalendarChangeRow;
  expected: number;
  completed: number;
  appliedMessages: string[];
  skippedMessages: string[];
  failedMessages: string[];
  workLocationId?: string | null;
  leaveId?: string | null;
}

export function isCalendarRowChanged(row: CalendarChangeRow): boolean {
  if (row.category !== row.originalCategory)
    return true;

  if (row.category === 'none')
    return false;

  if (row.entryType !== row.originalEntryType)
    return true;

  if (row.entryType === DayEntryType.SpecificHours && row.specificHours !== row.originalSpecificHours)
    return true;

  return isLeaveCategory(row.category) && row.leaveType !== row.originalLeaveType;
}

export function planCalendarChanges(rows: readonly CalendarChangeRow[]): CalendarChangeSet {
  const changedRows = rows.filter(isCalendarRowChanged);
  const workCreates: CalendarChangeSet['workCreates'] = [];
  const leaveCreates: CalendarChangeSet['leaveCreates'] = [];
  const workDeletes: CalendarChangeSet['workDeletes'] = [];
  const leaveDeletes: CalendarChangeSet['leaveDeletes'] = [];
  const workUpdates: CalendarChangeSet['workUpdates'] = [];
  const leaveUpdates: CalendarChangeSet['leaveUpdates'] = [];

  for (const row of changedRows) {
    const workPayload = (): CreateWorkLocationRequest => ({
      workDate: row.dateIso,
      workLocation: row.category === 'office' ? WorkLocationType.Office : WorkLocationType.Wfh,
      entryType: row.entryType,
      specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
      notes: null,
    });
    const leavePayload = (): CreateLeaveRequest => ({
      leaveDate: row.dateIso,
      leaveType: row.leaveType,
      entryType: row.entryType,
      specificHours: row.entryType === DayEntryType.SpecificHours ? row.specificHours : null,
      notes: null,
    });

    if (isWorkCategory(row.category) && !row.workLocationId)
      workCreates.push({ dateIso: row.dateIso, payload: workPayload() });

    if (isLeaveCategory(row.category) && !row.leaveId)
      leaveCreates.push({ dateIso: row.dateIso, payload: leavePayload() });

    if (row.workLocationId && !isWorkCategory(row.category))
      workDeletes.push({ dateIso: row.dateIso, id: row.workLocationId });

    if (row.leaveId && !isLeaveCategory(row.category))
      leaveDeletes.push({ dateIso: row.dateIso, id: row.leaveId });

    if (row.workLocationId && isWorkCategory(row.originalCategory) && isWorkCategory(row.category))
      workUpdates.push({ dateIso: row.dateIso, id: row.workLocationId, payload: workPayload() });

    if (row.leaveId && isLeaveCategory(row.originalCategory) && isLeaveCategory(row.category))
      leaveUpdates.push({ dateIso: row.dateIso, id: row.leaveId, payload: leavePayload() });
  }

  return {
    changedDateIsos: changedRows.map((row) => row.dateIso),
    workCreates,
    leaveCreates,
    workDeletes,
    leaveDeletes,
    workUpdates,
    leaveUpdates,
  };
}

export function reconcileCalendarChanges(
  rows: readonly CalendarChangeRow[],
  plan: CalendarChangeSet,
  execution: CalendarChangeExecution,
): CalendarReconciliation {
  const rowsByDate = new Map(rows.map((row) => [row.dateIso, row]));
  const outcomes = new Map<string, MutableRowOutcome>();
  const operationGroups = [
    plan.workCreates,
    plan.leaveCreates,
    plan.workDeletes,
    plan.leaveDeletes,
    plan.workUpdates,
    plan.leaveUpdates,
  ];

  for (const operations of operationGroups) {
    for (const operation of operations) {
      const row = rowsByDate.get(operation.dateIso);
      if (!row)
        continue;

      const outcome = outcomes.get(operation.dateIso) ?? createOutcome(row);
      outcome.expected += 1;
      outcomes.set(operation.dateIso, outcome);
    }
  }

  const workCreateResults = new Map(execution.workCreates.results.map((result) => [toDateKey(result.workDate), result]));
  for (const operation of plan.workCreates) {
    const outcome = outcomes.get(operation.dateIso);
    if (!outcome)
      continue;

    const result = workCreateResults.get(operation.dateIso);
    if (result?.status === 'Created' && result.entry) {
      applySuccess(outcome, `Created ${result.entry.workLocation === WorkLocationType.Office ? 'Office' : 'WFH'} entry.`);
      outcome.workLocationId = result.entry.id;
    } else if (result?.status === 'SkippedDuplicate') {
      applySkipped(outcome, result.message ?? 'Skipped duplicate work-location entry.');
    } else {
      applyFailure(outcome, result?.message ?? execution.workCreateError ?? 'Work-location create failed.');
    }
  }

  const leaveCreateResults = new Map(execution.leaveCreates.results.map((result) => [toDateKey(result.leaveDate), result]));
  for (const operation of plan.leaveCreates) {
    const outcome = outcomes.get(operation.dateIso);
    if (!outcome)
      continue;

    const result = leaveCreateResults.get(operation.dateIso);
    if (result?.status === 'Created' && result.entry) {
      applySuccess(outcome, 'Created leave entry.');
      outcome.leaveId = result.entry.id;
    } else if (result?.status === 'SkippedDuplicate') {
      applySkipped(outcome, result.message ?? 'Skipped duplicate leave entry.');
    } else {
      applyFailure(outcome, result?.message ?? execution.leaveCreateError ?? 'Leave create failed.');
    }
  }

  applyOperationResults(outcomes, plan.workDeletes, execution.workDeletes, 'Removed existing work-location entry.', 'workLocationId');
  applyOperationResults(outcomes, plan.leaveDeletes, execution.leaveDeletes, 'Removed existing leave entry.', 'leaveId');
  applyOperationResults(outcomes, plan.workUpdates, execution.workUpdates, 'Updated existing work-location entry.');
  applyOperationResults(outcomes, plan.leaveUpdates, execution.leaveUpdates, 'Updated existing leave entry.');

  let added = 0;
  let updated = 0;
  let skipped = 0;
  let failed = 0;
  const reconciledRows: CalendarRowReconciliation[] = [];

  for (const outcome of outcomes.values()) {
    const syncOriginal = outcome.completed === outcome.expected
      && outcome.failedMessages.length === 0
      && outcome.skippedMessages.length === 0;
    let state: CalendarRowReconciliation['state'];
    let message: string;

    if (outcome.failedMessages.length > 0) {
      state = 'failed';
      message = outcome.failedMessages[0];
      failed += outcome.failedMessages.length;
    } else if (outcome.skippedMessages.length > 0) {
      state = 'skipped';
      message = outcome.skippedMessages[0];
      skipped += outcome.skippedMessages.length;
    } else {
      state = 'applied';
      message = outcome.appliedMessages.length === 1 ? outcome.appliedMessages[0] : 'Applied calendar changes.';
      if (outcome.row.originalCategory === 'none')
        added += 1;
      else
        updated += 1;
    }

    reconciledRows.push({
      dateIso: outcome.row.dateIso,
      state,
      message,
      syncOriginal,
      ...(outcome.workLocationId !== undefined ? { workLocationId: outcome.workLocationId } : {}),
      ...(outcome.leaveId !== undefined ? { leaveId: outcome.leaveId } : {}),
    });
  }

  return { rows: reconciledRows, added, updated, skipped, failed };
}

function applyOperationResults<TPayload>(
  outcomes: Map<string, MutableRowOutcome>,
  operations: ReadonlyArray<CalendarPersistedOperation<TPayload>>,
  results: readonly CalendarOperationResult[],
  successMessage: string,
  clearedId?: 'workLocationId' | 'leaveId',
): void {
  const resultsByDate = new Map(results.map((result) => [result.dateIso, result]));
  for (const operation of operations) {
    const outcome = outcomes.get(operation.dateIso);
    if (!outcome)
      continue;

    const result = resultsByDate.get(operation.dateIso);
    if (result?.ok) {
      applySuccess(outcome, successMessage);
      if (clearedId)
        outcome[clearedId] = null;
    } else {
      applyFailure(outcome, result?.message ?? 'Calendar operation failed.');
    }
  }
}

function createOutcome(row: CalendarChangeRow): MutableRowOutcome {
  return {
    row,
    expected: 0,
    completed: 0,
    appliedMessages: [],
    skippedMessages: [],
    failedMessages: [],
  };
}

function applySuccess(outcome: MutableRowOutcome, message: string): void {
  outcome.completed += 1;
  outcome.appliedMessages.push(message);
}

function applySkipped(outcome: MutableRowOutcome, message: string): void {
  outcome.completed += 1;
  outcome.skippedMessages.push(message);
}

function applyFailure(outcome: MutableRowOutcome, message: string): void {
  outcome.completed += 1;
  outcome.failedMessages.push(message);
}

function isWorkCategory(category: CalendarDayCategory): boolean {
  return category === 'wfh' || category === 'office';
}

function isLeaveCategory(category: CalendarDayCategory): boolean {
  return category === 'annual' || category === 'sick';
}

function toDateKey(value: string): string {
  const raw = (value ?? '').trim();
  const tIndex = raw.indexOf('T');
  if (tIndex > 0)
    return raw.slice(0, tIndex);

  return raw.length >= 10 ? raw.slice(0, 10) : raw;
}
