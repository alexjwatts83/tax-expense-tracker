import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ExpenseList } from './expense-list';

describe('ExpenseList filters', () => {
	let httpTestingController: HttpTestingController;

	beforeEach(async () => {
		await TestBed.configureTestingModule({
			imports: [ExpenseList],
			providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
		}).compileComponents();

		httpTestingController = TestBed.inject(HttpTestingController);
	});

	afterEach(() => {
		httpTestingController.verify();
	});

	it('sends zero as the price filter parameter', () => {
		const component = TestBed.createComponent(ExpenseList).componentInstance;
		component.filterForm.patchValue({ price: 0 });

		component.applyFilters();

		const request = httpTestingController.expectOne(
			(candidate) => candidate.url === '/api/expenses/filter' && candidate.params.get('price') === '0',
		);
		expect(request.request.method).toBe('GET');
		request.flush([]);
	});

	it('omits the price parameter when the filter is blank', () => {
		const component = TestBed.createComponent(ExpenseList).componentInstance;

		component.applyFilters();

		const request = httpTestingController.expectOne('/api/expenses?page=1&pageSize=20');
		expect(request.request.params.has('price')).toBe(false);
		request.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 });
	});
});