import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';
import { DataTransferService } from '../../services/data-transfer';
import { DataTransfer } from './data-transfer';

describe('DataTransfer', () => {
	const dataTransferService = {
		export: vi.fn(),
		import: vi.fn(),
	};

	beforeEach(async () => {
		dataTransferService.export.mockReset();
		dataTransferService.import.mockReset();

		await TestBed.configureTestingModule({
			imports: [DataTransfer],
			providers: [{ provide: DataTransferService, useValue: dataTransferService }],
		}).compileComponents();
	});

	it('renders a ProblemDetails message returned as a Blob export error', async () => {
		const problem = new Blob(
			[JSON.stringify({ code: 'server_error', detail: 'The export could not be completed.' })],
			{ type: 'application/problem+json' },
		);
		dataTransferService.export.mockReturnValue(
			throwError(() => new HttpErrorResponse({ status: 500, error: problem })),
		);
		const fixture = TestBed.createComponent(DataTransfer);
		fixture.detectChanges();

		fixture.componentInstance.downloadExport();
		await vi.waitFor(() =>
			expect(fixture.componentInstance.errorMessage).toBe('The export could not be completed.'),
		);
		expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
			'The export could not be completed.',
		);
	});

	it('renders a ProblemDetails message returned as a JSON import error', async () => {
		dataTransferService.import.mockReturnValue(
			throwError(
				() =>
					new HttpErrorResponse({
						status: 409,
						error: {
							code: 'conflict',
							detail: 'The import conflicts with existing data.',
						},
					}),
			),
		);
		const fixture = TestBed.createComponent(DataTransfer);
		fixture.componentInstance.parsedPayload = {};
		fixture.detectChanges();

		fixture.componentInstance.runImport();
		await vi.waitFor(() =>
			expect(fixture.componentInstance.errorMessage).toBe('The import conflicts with existing data.'),
		);
		expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain(
			'The import conflicts with existing data.',
		);
	});
});