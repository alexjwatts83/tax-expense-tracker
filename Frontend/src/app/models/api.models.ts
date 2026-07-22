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
  item: string;
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

export interface CreateExpenseRequest {
  item: string;
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