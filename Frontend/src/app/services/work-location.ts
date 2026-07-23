import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
	CreateWorkLocationRequest,
	WorkLocationBatchCreateRequest,
	WorkLocationBatchCreateResult,
	DateRangeRequest,
	DayEntrySummary,
	WorkLocationEntry,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class WorkLocationService {
	private readonly apiUrl = '/api/work-locations';

	constructor(private readonly http: HttpClient) {}

	getAll(request?: DateRangeRequest): Observable<WorkLocationEntry[]> {
		const params = this.buildDateRangeParams(request);
		return this.http.get<WorkLocationEntry[]>(this.apiUrl, { params });
	}

	getById(id: string): Observable<WorkLocationEntry> {
		return this.http.get<WorkLocationEntry>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateWorkLocationRequest): Observable<WorkLocationEntry> {
		return this.http.post<WorkLocationEntry>(this.apiUrl, payload);
	}

	createBatch(payload: WorkLocationBatchCreateRequest): Observable<WorkLocationBatchCreateResult> {
		return this.http.post<WorkLocationBatchCreateResult>(`${this.apiUrl}/batch`, payload);
	}

	update(id: string, payload: CreateWorkLocationRequest): Observable<WorkLocationEntry> {
		return this.http.put<WorkLocationEntry>(`${this.apiUrl}/${id}`, payload);
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