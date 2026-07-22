import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
	CreateExpenseRequest,
	Expense,
	ExpenseFilterRequest,
	ExpenseSummary,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
	private readonly apiUrl = '/api/expenses';

	constructor(private readonly http: HttpClient) {}

	getAll(page = 1, pageSize = 20): Observable<Expense[]> {
		const params = new HttpParams()
			.set('page', page)
			.set('pageSize', pageSize);

		return this.http.get<Expense[]>(this.apiUrl, { params });
	}

	getById(id: string): Observable<Expense> {
		return this.http.get<Expense>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateExpenseRequest): Observable<Expense> {
		return this.http.post<Expense>(this.apiUrl, payload);
	}

	update(id: string, payload: CreateExpenseRequest): Observable<Expense> {
		return this.http.put<Expense>(`${this.apiUrl}/${id}`, payload);
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`);
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {});
	}

	getSummary(): Observable<ExpenseSummary> {
		return this.http.get<ExpenseSummary>(`${this.apiUrl}/summary`);
	}

	filter(request: ExpenseFilterRequest): Observable<Expense[]> {
		let params = new HttpParams();

		if (request.date) {
			params = params.set('date', request.date);
		}

		if (request.bank) {
			params = params.set('bank', request.bank);
		}

		if (request.price !== undefined) {
			params = params.set('price', request.price);
		}

		if (request.sourceId) {
			params = params.set('sourceId', request.sourceId);
		}

		if (request.tagIds && request.tagIds.length > 0) {
			params = params.set('tagIds', request.tagIds.join(','));
		}

		return this.http.get<Expense[]>(`${this.apiUrl}/filter`, { params });
	}
}
