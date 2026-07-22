import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Bank, CreateBankRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BankService {
	private readonly apiUrl = '/api/banks';

	constructor(private readonly http: HttpClient) {}

	getAll(): Observable<Bank[]> {
		return this.http.get<Bank[]>(this.apiUrl);
	}

	getById(id: string): Observable<Bank> {
		return this.http.get<Bank>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateBankRequest): Observable<Bank> {
		return this.http.post<Bank>(this.apiUrl, payload);
	}

	update(id: string, payload: CreateBankRequest): Observable<Bank> {
		return this.http.put<Bank>(`${this.apiUrl}/${id}`, payload);
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`);
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {});
	}
}
