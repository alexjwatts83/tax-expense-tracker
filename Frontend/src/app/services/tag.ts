import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay, tap } from 'rxjs';
import { CreateTagRequest, Tag } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class TagService {
	private readonly apiUrl = '/api/tags';
	private cachedTags: Observable<Tag[]> | null = null;

	constructor(private readonly http: HttpClient) {}

	getAll(): Observable<Tag[]> {
		this.cachedTags ??= this.http
			.get<Tag[]>(this.apiUrl)
			.pipe(shareReplay({ bufferSize: 1, refCount: false }));
		return this.cachedTags;
	}

	getById(id: string): Observable<Tag> {
		return this.http.get<Tag>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateTagRequest): Observable<Tag> {
		return this.http.post<Tag>(this.apiUrl, payload).pipe(tap(() => this.invalidateCache()));
	}

	update(id: string, payload: CreateTagRequest): Observable<Tag> {
		return this.http.put<Tag>(`${this.apiUrl}/${id}`, payload).pipe(tap(() => this.invalidateCache()));
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(tap(() => this.invalidateCache()));
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {}).pipe(tap(() => this.invalidateCache()));
	}

	private invalidateCache(): void {
		this.cachedTags = null;
	}
}
