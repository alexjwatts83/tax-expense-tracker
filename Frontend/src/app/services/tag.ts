import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateTagRequest, Tag } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class TagService {
	private readonly apiUrl = '/api/tags';

	constructor(private readonly http: HttpClient) {}

	getAll(): Observable<Tag[]> {
		return this.http.get<Tag[]>(this.apiUrl);
	}

	getById(id: string): Observable<Tag> {
		return this.http.get<Tag>(`${this.apiUrl}/${id}`);
	}

	create(payload: CreateTagRequest): Observable<Tag> {
		return this.http.post<Tag>(this.apiUrl, payload);
	}

	update(id: string, payload: CreateTagRequest): Observable<Tag> {
		return this.http.put<Tag>(`${this.apiUrl}/${id}`, payload);
	}

	softDelete(id: string): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/${id}`);
	}

	restore(id: string): Observable<void> {
		return this.http.post<void>(`${this.apiUrl}/${id}/restore`, {});
	}
}
