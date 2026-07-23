import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
	CreateLeaveRequest,
	LeaveBatchCreateRequest,
	LeaveBatchCreateResult,
	DateRangeRequest,
	DayEntrySummary,
	LeaveEntry,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class LeaveService {
	private readonly apiUrl = '/api/leave';

	constructor(private readonly http: HttpClient) {}

	getAll(request?: DateRangeRequest): Observable<LeaveEntry[]> {
		const params = this.buildDateRangeParams(request);
		return this.http.get<LeaveEntry[]>(this.apiUrl, { params });
	}

	getById(id: string): Observable<LeaveEntry> {
		return this.http.get<LeaveEntry>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateLeaveRequest): Observable<LeaveEntry> {
		return this.http.post<LeaveEntry>(this.apiUrl, payload);
	}

	createBatch(payload: LeaveBatchCreateRequest): Observable<LeaveBatchCreateResult> {
		return this.http.post<LeaveBatchCreateResult>(`${this.apiUrl}/batch`, payload);
	}

	update(id: string, payload: CreateLeaveRequest): Observable<LeaveEntry> {
		return this.http.put<LeaveEntry>(`${this.apiUrl}/${id}`, payload);
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