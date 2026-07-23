import { Routes } from '@angular/router';
import { BankManagement } from './components/bank-management/bank-management';
import { Dashboard } from './components/dashboard/dashboard';
import { ExpenseDetails } from './components/expense-details/expense-details';
import { ExpenseForm } from './components/expense-form/expense-form';
import { ExpenseList } from './components/expense-list/expense-list';
import { LeaveManagement } from './components/leave-management/leave-management';
import { PublicHolidayManagement } from './components/public-holiday-management/public-holiday-management';
import { TagManagement } from './components/tag-management/tag-management';
import { TrackerManagement } from './components/tracker-management/tracker-management';
import { WorkFromHomeManagement } from './components/work-from-home-management/work-from-home-management';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'dashboard' },
	{ path: 'dashboard', component: Dashboard },
	{ path: 'expenses', component: ExpenseList },
	{ path: 'expenses/new', component: ExpenseForm },
	{ path: 'expenses/:id', component: ExpenseDetails },
	{ path: 'trackers', component: TrackerManagement },
	{ path: 'tags', component: TagManagement },
	{ path: 'banks', component: BankManagement },
	{ path: 'work-from-home', component: WorkFromHomeManagement },
	{ path: 'leave', component: LeaveManagement },
	{ path: 'public-holidays', component: PublicHolidayManagement },
];
