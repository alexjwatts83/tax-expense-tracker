import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay, tap } from 'rxjs';
import { Bank, CreateBankRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BankService {
	private readonly apiUrl = '/api/banks';
	private cachedBanks: Observable<Bank[]> | null = null;

	constructor(private readonly http: HttpClient) {}

	getAll(): Observable<Bank[]> {
		this.cachedBanks ??= this.http
			.get<Bank[]>(this.apiUrl)
			.pipe(shareReplay({ bufferSize: 1, refCount: false }));
		return this.cachedBanks;
	}

	getById(id: string): Observable<Bank> {
		return this.http.get<Bank>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateBankRequest): Observable<Bank> {
		return this.http.post<Bank>(this.apiUrl, payload).pipe(tap(() => this.invalidateCache()));
	}

	update(id: string, payload: CreateBankRequest): Observable<Bank> {
		return this.http.put<Bank>(`${this.apiUrl}/${id}`, payload).pipe(tap(() => this.invalidateCache()));
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(tap(() => this.invalidateCache()));
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {}).pipe(tap(() => this.invalidateCache()));
	}

	private invalidateCache(): void {
		this.cachedBanks = null;
	}
}
