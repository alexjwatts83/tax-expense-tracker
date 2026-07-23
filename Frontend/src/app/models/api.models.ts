export interface Tracker {
  id: string;
  name: string;
  description?: string | null;
  createdAt: string;
}

export interface Tag {
  id: string;
  name: string;
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

export interface DayEntryHoliday {
  date: string;
  name: string;
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

export interface WorkFromHomeEntry {
  id: string;
  workDate: string;
  entryType: DayEntryType;
  hoursWorked: number;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWorkFromHomeRequest {
  workDate: string;
  entryType: DayEntryType;
  specificHours?: number | null;
  notes?: string | null;
}

export interface LeaveEntry {
  id: string;
  leaveDate: string;
  entryType: DayEntryType;
  hoursWorked: number;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeaveRequest {
  leaveDate: string;
  entryType: DayEntryType;
  specificHours?: number | null;
  notes?: string | null;
}

export interface PublicHoliday {
  id: string;
  holidayDate: string;
  name: string;
  source?: string | null;
  isImported: boolean;
  createdAt: string;
}

export interface PublicHolidayImportResult {
  importedCount: number;
  skippedDuplicateCount: number;
  warnings: string[];
}