import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
	DataTransferExportKind,
	DataTransferImportKind,
	DataTransferImportMode,
	DataTransferImportResult,
} from '../models/data-transfer.models';

@Injectable({ providedIn: 'root' })
export class DataTransferService {
	private readonly http = inject(HttpClient);
	private readonly apiUrl = '/api/data-transfer';

	export(kind: DataTransferExportKind, includeSoftDeleted: boolean): Observable<Blob> {
		const endpoint = kind === 'reference' ? 'export' : `export/${kind}`;
		const params = new HttpParams().set('includeSoftDeleted', includeSoftDeleted);
		return this.http.get(`${this.apiUrl}/${endpoint}`, { params, responseType: 'blob' });
	}

	import(
		kind: DataTransferImportKind,
		payload: unknown,
		mode: DataTransferImportMode,
		dryRun: boolean,
		allowDeletes: boolean,
	): Observable<DataTransferImportResult> {
		const endpoint = kind === 'reference' ? 'import' : `import/${kind}`;
		const params = new HttpParams()
			.set('mode', mode)
			.set('dryRun', dryRun)
			.set('allowDeletes', allowDeletes);

		return this.http.post<DataTransferImportResult>(`${this.apiUrl}/${endpoint}`, payload, { params });
	}
}