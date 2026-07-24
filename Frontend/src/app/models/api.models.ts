export interface Tracker {
  id: string;
  name: string;
  description?: string | null;
  createdAt: string;
}

export interface Tag {
  id: string;
  name: string;
  color: string;
  createdAt: string;
}

export interface Bank {
  id: string;
  name: string;
  createdAt: string;
}

export interface Expense {
  id: string;
  description: string;
  date: string;
  bankId: string;
  bank?: Bank;
  price: number;
  sourceId: string;
  source?: Tracker;
  tags: Tag[];
  createdAt: string;
  updatedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CreateExpenseRequest {
  description: string;
  date: string;
  bankId: string;
  price: number;
  sourceId: string;
  tagIds: string[];
}

export interface CreateTrackerRequest {
  name: string;
  description?: string;
}

export interface CreateTagRequest {
  name: string;
  color?: string;
}

export interface CreateBankRequest {
  name: string;
}

export interface SummaryGroup {
  total: number;
  bank?: string;
  source?: string;
}

export interface ExpenseSummary {
  totalSpent: number;
  byBank: SummaryGroup[];
  bySource: SummaryGroup[];
}

export interface ExpenseFilterRequest {
  date?: string;
  bankId?: string;
  price?: number;
  sourceId?: string;
  tagIds?: string[];
}

export enum DayEntryType {
  FullDay = 1,
  HalfDay = 2,
  SpecificHours = 3,
}

export enum LeaveType {
  Annual = 1,
  Sick = 2,
}

export enum WorkLocationType {
  Wfh = 1,
  Office = 2,
}

export interface DayEntryHoliday {
  date: string;
  name: string;
  canBeWorkedOn: boolean;
}

export interface DayEntrySummary {
  view: string;
  anchorDate: string;
  fromDate: string;
  toDate: string;
  totalHours: number;
  totalDays: number;
  entryCount: number;
  holidays: DayEntryHoliday[];
}

export interface DateRangeRequest {
  fromDate?: string;
  toDate?: string;
}

export interface WorkLocationEntry {
  id: string;
  workDate: string;
  workLocation: WorkLocationType;
  entryType: DayEntryType;
  hoursWorked: number;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWorkLocationRequest {
  workDate: string;
  workLocation: WorkLocationType;
  entryType: DayEntryType;
  specificHours?: number | null;
  notes?: string | null;
}

export interface WorkLocationBatchCreateRequest {
  items: CreateWorkLocationRequest[];
}

export interface WorkLocationBatchItemResult {
  workDate: string;
  workLocation: WorkLocationType;
  entryType: DayEntryType;
  specificHours?: number | null;
  notes?: string | null;
  status: string;
  message?: string | null;
  entry?: WorkLocationEntry | null;
}

export interface WorkLocationBatchCreateResult {
  totalRequested: number;
  createdCount: number;
  skippedCount: number;
  failedCount: number;
  results: WorkLocationBatchItemResult[];
}

export interface LeaveEntry {
  id: string;
  leaveDate: string;
  leaveType: LeaveType;
  entryType: DayEntryType;
  hoursWorked: number;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeaveRequest {
  leaveDate: string;
  leaveType: LeaveType;
  entryType: DayEntryType;
  specificHours?: number | null;
  notes?: string | null;
}

export interface LeaveBatchCreateRequest {
  items: CreateLeaveRequest[];
}

export interface LeaveBatchItemResult {
  leaveDate: string;
  leaveType: LeaveType;
  entryType: DayEntryType;
  specificHours?: number | null;
  notes?: string | null;
  status: string;
  message?: string | null;
  entry?: LeaveEntry | null;
}

export interface LeaveBatchCreateResult {
  totalRequested: number;
  createdCount: number;
  skippedCount: number;
  failedCount: number;
  results: LeaveBatchItemResult[];
}

export interface PublicHoliday {
  id: string;
  holidayDate: string;
  name: string;
  source?: string | null;
  isImported: boolean;
  canBeWorkedOn: boolean;
  createdAt: string;
}

export interface UpdatePublicHolidayRequest {
  holidayDate: string;
  name: string;
  source?: string | null;
  canBeWorkedOn: boolean;
}

export interface PublicHolidayImportResult {
  importedCount: number;
  skippedDuplicateCount: number;
  warnings: string[];
}