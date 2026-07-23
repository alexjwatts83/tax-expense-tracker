import { Routes } from '@angular/router';
import { BankManagement } from './components/bank-management/bank-management';
import { Dashboard } from './components/dashboard/dashboard';
import { ExpenseDetails } from './components/expense-details/expense-details';
import { ExpenseForm } from './components/expense-form/expense-form';
import { ExpenseList } from './components/expense-list/expense-list';
import { PublicHolidayManagement } from './components/public-holiday-management/public-holiday-management';
import { TagManagement } from './components/tag-management/tag-management';
import { TimeTracking } from './components/time-tracking/time-tracking';
import { TrackerManagement } from './components/tracker-management/tracker-management';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'dashboard' },
	{ path: 'dashboard', component: Dashboard },
	{ path: 'expenses', component: ExpenseList },
	{ path: 'expenses/new', component: ExpenseForm },
	{ path: 'expenses/:id', component: ExpenseDetails },
	{ path: 'time-tracking', component: TimeTracking },
	{ path: 'work-from-home', redirectTo: 'time-tracking', pathMatch: 'full' },
	{ path: 'leave', redirectTo: 'time-tracking', pathMatch: 'full' },
	{ path: 'trackers', component: TrackerManagement },
	{ path: 'tags', component: TagManagement },
	{ path: 'banks', component: BankManagement },
	{ path: 'public-holidays', component: PublicHolidayManagement },
];
