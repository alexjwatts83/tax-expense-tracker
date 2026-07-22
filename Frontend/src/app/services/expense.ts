import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateExpenseRequest, Expense } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ExpenseService {
	private readonly apiUrl = 'http://localhost:5000/api/expenses';

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
}
