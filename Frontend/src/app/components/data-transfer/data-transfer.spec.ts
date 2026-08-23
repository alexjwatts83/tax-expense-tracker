import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, throwError } from 'rxjs';
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

	it('cancels an active export request', async () => {
		const teardown = vi.fn();
		dataTransferService.export.mockReturnValue(new Observable(() => teardown));
		const fixture = TestBed.createComponent(DataTransfer);
		fixture.detectChanges();

		fixture.componentInstance.downloadExport();
		await fixture.whenStable();
		fixture.detectChanges();

		expect(fixture.componentInstance.isExporting).toBe(true);
		expect(fixture.componentInstance.infoMessage).toBe('Downloading export...');
		expect((fixture.nativeElement as HTMLElement).querySelector('.info-message[aria-live="polite"]')?.textContent).toContain(
			'Downloading export...',
		);
		expect((fixture.nativeElement as HTMLElement).textContent).toContain('Cancel');

		fixture.componentInstance.cancelExport();

		expect(teardown).toHaveBeenCalledOnce();
		expect(fixture.componentInstance.isExporting).toBe(false);
		expect(fixture.componentInstance.infoMessage).toBe('Export cancelled.');
	});

	it('cancels an active validation request', () => {
		const teardown = vi.fn();
		dataTransferService.import.mockReturnValue(new Observable(() => teardown));
		const fixture = TestBed.createComponent(DataTransfer);
		fixture.componentInstance.parsedPayload = {};
		fixture.componentInstance.validationPassed = true;
		fixture.detectChanges();

		fixture.componentInstance.runImport();

		expect(fixture.componentInstance.isImporting).toBe(true);
		expect(fixture.componentInstance.infoMessage).toBe('Validating import...');

		fixture.componentInstance.cancelImport();

		expect(teardown).toHaveBeenCalledOnce();
		expect(fixture.componentInstance.isImporting).toBe(false);
		expect(fixture.componentInstance.validationPassed).toBe(false);
		expect(fixture.componentInstance.infoMessage).toBe('Validation cancelled.');
	});

	it('rejects a file larger than 10 MiB without reading it', async () => {
		const fixture = TestBed.createComponent(DataTransfer);
		fixture.detectChanges();
		const file = new File(['{}'], 'big.json', { type: 'application/json' });
		Object.defineProperty(file, 'size', { value: 10 * 1024 * 1024 + 1 });
		const textSpy = vi.spyOn(file, 'text');

		await fixture.componentInstance.selectFile({ target: { files: [file] } } as unknown as Event);

		expect(textSpy).not.toHaveBeenCalled();
		expect(fixture.componentInstance.parsedPayload).toBeNull();
		expect(fixture.componentInstance.selectedFile).toBeNull();
		expect(fixture.componentInstance.errorMessage).toBe('The selected file exceeds the 10 MiB limit.');
	});

	it('rejects a non-JSON file without reading it', async () => {
		const fixture = TestBed.createComponent(DataTransfer);
		fixture.detectChanges();
		const file = new File(['plain text'], 'notes.txt', { type: 'text/plain' });
		const textSpy = vi.spyOn(file, 'text');

		await fixture.componentInstance.selectFile({ target: { files: [file] } } as unknown as Event);

		expect(textSpy).not.toHaveBeenCalled();
		expect(fixture.componentInstance.parsedPayload).toBeNull();
		expect(fixture.componentInstance.selectedFile).toBeNull();
		expect(fixture.componentInstance.errorMessage).toBe('Only .json files are supported.');
	});

	it('accepts a valid JSON file within the limit', async () => {
		const fixture = TestBed.createComponent(DataTransfer);
		fixture.detectChanges();
		const file = new File(['{"data":{}}'], 'reference.json', { type: 'application/json' });

		await fixture.componentInstance.selectFile({ target: { files: [file] } } as unknown as Event);

		expect(fixture.componentInstance.parsedPayload).toEqual({ data: {} });
		expect(fixture.componentInstance.selectedFile).toBe(file);
		expect(fixture.componentInstance.errorMessage).toBe('');
	});
});