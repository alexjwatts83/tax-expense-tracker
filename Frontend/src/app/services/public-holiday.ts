import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
	DateRangeRequest,
	PublicHoliday,
	PublicHolidayImportResult,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class PublicHolidayService {
	private readonly apiUrl = '/api/public-holidays';

	constructor(private readonly http: HttpClient) {}

	getAll(request?: DateRangeRequest): Observable<PublicHoliday[]> {
		const params = this.buildDateRangeParams(request);
		return this.http.get<PublicHoliday[]>(this.apiUrl, { params });
	}

	import(file: File, source?: string): Observable<PublicHolidayImportResult> {
		const formData = new FormData();
		formData.append('file', file, file.name);

		let params = new HttpParams();
		if (source?.trim()) {
			params = params.set('source', source.trim());
		}

		return this.http.post<PublicHolidayImportResult>(`${this.apiUrl}/import`, formData, { params });
	}

	setWorkable(id: string, canBeWorkedOn: boolean): Observable<PublicHoliday> {
		return this.http.patch<PublicHoliday>(`${this.apiUrl}/${id}/workable`, { canBeWorkedOn });
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