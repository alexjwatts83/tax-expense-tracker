import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateTrackerRequest, Tracker } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class TrackerService {
	private readonly apiUrl = '/api/trackers';

	constructor(private readonly http: HttpClient) {}

	getAll(): Observable<Tracker[]> {
		return this.http.get<Tracker[]>(this.apiUrl);
	}

	getById(id: string): Observable<Tracker> {
		return this.http.get<Tracker>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateTrackerRequest): Observable<Tracker> {
		return this.http.post<Tracker>(this.apiUrl, payload);
	}

	update(id: string, payload: CreateTrackerRequest): Observable<Tracker> {
		return this.http.put<Tracker>(`${this.apiUrl}/${id}`, payload);
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`);
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {});
	}
}
