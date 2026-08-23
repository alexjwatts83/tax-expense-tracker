import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay, tap } from 'rxjs';
import { CreateTrackerRequest, Tracker } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class TrackerService {
	private readonly apiUrl = '/api/trackers';
	private cachedTrackers: Observable<Tracker[]> | null = null;

	constructor(private readonly http: HttpClient) {}

	getAll(): Observable<Tracker[]> {
		this.cachedTrackers ??= this.http
			.get<Tracker[]>(this.apiUrl)
			.pipe(shareReplay({ bufferSize: 1, refCount: false }));
		return this.cachedTrackers;
	}

	getById(id: string): Observable<Tracker> {
		return this.http.get<Tracker>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateTrackerRequest): Observable<Tracker> {
		return this.http.post<Tracker>(this.apiUrl, payload).pipe(tap(() => this.invalidateCache()));
	}

	update(id: string, payload: CreateTrackerRequest): Observable<Tracker> {
		return this.http.put<Tracker>(`${this.apiUrl}/${id}`, payload).pipe(tap(() => this.invalidateCache()));
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(tap(() => this.invalidateCache()));
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {}).pipe(tap(() => this.invalidateCache()));
	}

	private invalidateCache(): void {
		this.cachedTrackers = null;
	}
}
