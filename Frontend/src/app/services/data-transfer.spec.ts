import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DataTransferService } from './data-transfer';

describe('DataTransferService', () => {
	let httpTestingController: HttpTestingController;
	let service: DataTransferService;

	beforeEach(() => {
		TestBed.configureTestingModule({
			providers: [DataTransferService, provideHttpClient(), provideHttpClientTesting()],
		});

		httpTestingController = TestBed.inject(HttpTestingController);
		service = TestBed.inject(DataTransferService);
	});

	afterEach(() => {
		httpTestingController.verify();
	});

	it('requests an entity export as a blob with the soft-delete option', () => {
		const response = new Blob(['[]'], { type: 'application/json' });

		service.export('trackers', false).subscribe((result) => expect(result).toBe(response));

		const request = httpTestingController.expectOne(
			(candidate) => candidate.url === '/api/data-transfer/export/trackers',
		);
		expect(request.request.method).toBe('GET');
		expect(request.request.responseType).toBe('blob');
		expect(request.request.params.get('includeSoftDeleted')).toBe('false');
		request.flush(response);
	});

	it('posts an import payload with explicit mode and safety options', () => {
		const payload = { data: { trackers: [] } };
		const result = { dryRun: true, mode: 1, results: [] };

		service.import('reference', payload, 'Replace', true, true).subscribe((value) =>
			expect(value).toEqual(result),
		);

		const request = httpTestingController.expectOne((candidate) => candidate.url === '/api/data-transfer/import');
		expect(request.request.method).toBe('POST');
		expect(request.request.body).toEqual(payload);
		expect(request.request.params.get('mode')).toBe('Replace');
		expect(request.request.params.get('dryRun')).toBe('true');
		expect(request.request.params.get('allowDeletes')).toBe('true');
		request.flush(result);
	});
});