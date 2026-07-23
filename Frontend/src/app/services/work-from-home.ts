import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
	CreateWorkFromHomeRequest,
	WorkFromHomeBatchCreateRequest,
	WorkFromHomeBatchCreateResult,
	DateRangeRequest,
	DayEntrySummary,
	WorkFromHomeEntry,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class WorkFromHomeService {
	private readonly apiUrl = '/api/work-from-home';

	constructor(private readonly http: HttpClient) {}

	getAll(request?: DateRangeRequest): Observable<WorkFromHomeEntry[]> {
		const params = this.buildDateRangeParams(request);
		return this.http.get<WorkFromHomeEntry[]>(this.apiUrl, { params });
	}

	getById(id: string): Observable<WorkFromHomeEntry> {
		return this.http.get<WorkFromHomeEntry>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateWorkFromHomeRequest): Observable<WorkFromHomeEntry> {
		return this.http.post<WorkFromHomeEntry>(this.apiUrl, payload);
	}

	createBatch(payload: WorkFromHomeBatchCreateRequest): Observable<WorkFromHomeBatchCreateResult> {
		return this.http.post<WorkFromHomeBatchCreateResult>(`${this.apiUrl}/batch`, payload);
	}

	update(id: string, payload: CreateWorkFromHomeRequest): Observable<WorkFromHomeEntry> {
		return this.http.put<WorkFromHomeEntry>(`${this.apiUrl}/${id}`, payload);
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`);
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {});
	}

	getSummary(view: 'week' | 'month', date: string): Observable<DayEntrySummary> {
		const params = new HttpParams().set('view', view).set('date', date);
		return this.http.get<DayEntrySummary>(`${this.apiUrl}/summary`, { params });
	}

	private buildDateRangeParams(request?: DateRangeRequest): HttpParams {
		let params = new HttpParams();

		if (request?.fromDate) {
			params = params.set('fromDate', request.fromDate);
		}

		if (request?.toDate) {
			params = params.set('toDate', request.toDate);
		}

		return params;
	}
}