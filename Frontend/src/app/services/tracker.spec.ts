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

	it('reuses the reference-data response until a successful mutation invalidates it', () => {
		const trackers = [
			{
				id: '98a2478e-cc4e-4117-a2bb-6aa8ef0ee80e',
				name: 'Professional subscription',
				description: 'Annual plan',
				createdAt: '2026-08-02T00:00:00Z',
			},
		];

		service.getAll().subscribe((result) => expect(result).toEqual(trackers));
		service.getAll().subscribe((result) => expect(result).toEqual(trackers));
		const initialRequest = httpTestingController.expectOne('/api/trackers');
		initialRequest.flush(trackers);

		service.create({ name: 'New tracker' }).subscribe();
		const createRequest = httpTestingController.expectOne('/api/trackers');
		createRequest.flush({ ...trackers[0], id: 'c6547750-b990-44b1-ac89-d0e35cdfec20', name: 'New tracker' });

		service.getAll().subscribe();
		httpTestingController.expectOne('/api/trackers').flush(trackers);
	});
});