import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TrackerService } from './tracker';

describe('TrackerService', () => {
	let httpTestingController: HttpTestingController;
	let service: TrackerService;

	beforeEach(() => {
		TestBed.configureTestingModule({
			providers: [TrackerService, provideHttpClient(), provideHttpClientTesting()],
		});

		httpTestingController = TestBed.inject(HttpTestingController);
		service = TestBed.inject(TrackerService);
	});

	afterEach(() => {
		httpTestingController.verify();
	});

	it('sends tracker creation to the API', () => {
		const payload = { name: 'Professional subscription', description: 'Annual plan' };
		const tracker = {
			id: '98a2478e-cc4e-4117-a2bb-6aa8ef0ee80e',
			...payload,
			createdAt: '2026-08-02T00:00:00Z',
		};

		service.create(payload).subscribe((result) => expect(result).toEqual(tracker));

		const request = httpTestingController.expectOne('/api/trackers');
		expect(request.request.method).toBe('POST');
		expect(request.request.body).toEqual(payload);
		request.flush(tracker);
	});
});