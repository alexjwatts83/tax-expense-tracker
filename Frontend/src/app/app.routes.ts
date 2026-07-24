import { Routes } from '@angular/router';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'dashboard' },
	{
		path: 'dashboard',
		loadComponent: () =>
			import('./components/dashboard/dashboard').then((m) => m.Dashboard),
	},
	{
		path: 'expenses',
		loadComponent: () =>
			import('./components/expense-list/expense-list').then((m) => m.ExpenseList),
	},
	{
		path: 'expenses/new',
		loadComponent: () =>
			import('./components/expense-form/expense-form').then((m) => m.ExpenseForm),
	},
	{
		path: 'expenses/:id',
		loadComponent: () =>
			import('./components/expense-form/expense-form').then((m) => m.ExpenseForm),
	},
	{
		path: 'time-tracking',
		loadComponent: () =>
			import('./components/time-tracking/time-tracking').then((m) => m.TimeTracking),
	},
	{ path: 'work-locations', redirectTo: 'time-tracking', pathMatch: 'full' },
	{ path: 'leave', redirectTo: 'time-tracking', pathMatch: 'full' },
	{
		path: 'trackers',
		loadComponent: () =>
			import('./components/tracker-management/tracker-management').then((m) => m.TrackerManagement),
	},
	{
		path: 'tags',
		loadComponent: () =>
			import('./components/tag-management/tag-management').then((m) => m.TagManagement),
	},
	{
		path: 'banks',
		loadComponent: () =>
			import('./components/bank-management/bank-management').then((m) => m.BankManagement),
	},
	{
		path: 'public-holidays',
		loadComponent: () =>
			import('./components/public-holiday-management/public-holiday-management').then((m) => m.PublicHolidayManagement),
	},
	{
		path: 'calendar-batch-entry',
		loadComponent: () =>
			import('./components/calendar-batch-entry/calendar-batch-entry').then((m) => m.CalendarBatchEntry),
	},
];
