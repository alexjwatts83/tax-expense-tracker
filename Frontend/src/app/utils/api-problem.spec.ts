import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { classifyApiProblem, parseApiError } from './api-problem';

describe('API problem parsing', () => {
	it('parses a JSON problem with field errors and correlation ID', async () => {
		const error = new HttpErrorResponse({
			status: 400,
			error: {
				type: 'https://tax-expense-tracker.dev/problems/validation_error',
				title: 'Validation error',
				status: 400,
				detail: 'One or more validation errors occurred.',
				instance: '/api/trackers',
				code: 'validation_error',
				correlationId: 'json-correlation',
				errors: { name: ['Name is required.'] },
			},
		});

		const result = await parseApiError(error, 'Unable to save tracker.');

		expect(result.message).toBe('One or more validation errors occurred.');
		expect(result.classification).toBe('validation');
		expect(result.code).toBe('validation_error');
		expect(result.correlationId).toBe('json-correlation');
		expect(result.errors).toEqual({ name: ['Name is required.'] });
	});

	it('uses a plain-text response as the message and classifies by status', async () => {
		const error = new HttpErrorResponse({ status: 409, error: 'Expense already exists.' });

		const result = await parseApiError(error, 'Unable to save expense.');

		expect(result.message).toBe('Expense already exists.');
		expect(result.classification).toBe('conflict');
		expect(result.problem).toBeNull();
	});

	it('decodes a JSON problem returned as a Blob', async () => {
		const blob = new Blob(
			[
				JSON.stringify({
					status: 500,
					code: 'server_error',
					detail: 'The export could not be completed.',
					correlationId: 'blob-correlation',
				}),
			],
			{ type: 'application/problem+json' },
		);
		const error = new HttpErrorResponse({ status: 500, error: blob });

		const result = await parseApiError(error, 'Unable to export data.');

		expect(result.message).toBe('The export could not be completed.');
		expect(result.classification).toBe('server');
		expect(result.correlationId).toBe('blob-correlation');
	});

	it('uses plain text returned as a Blob', async () => {
		const error = new HttpErrorResponse({
			status: 503,
			error: new Blob(['Export service unavailable.'], { type: 'text/plain' }),
		});

		const result = await parseApiError(error, 'Unable to export data.');

		expect(result.message).toBe('Export service unavailable.');
		expect(result.classification).toBe('server');
	});

	it('falls back safely for malformed JSON and preserves a header correlation ID', async () => {
		const error = new HttpErrorResponse({
			status: 500,
			error: '{"detail":',
			headers: new HttpHeaders({ 'X-Correlation-ID': 'header-correlation' }),
		});

		const result = await parseApiError(error, 'Unable to load data.');

		expect(result.message).toBe('Unable to load data.');
		expect(result.classification).toBe('server');
		expect(result.correlationId).toBe('header-correlation');
	});

	it('classifies stable codes before contradictory HTTP statuses', () => {
		expect(classifyApiProblem('access_denied', 500)).toBe('authorization');
		expect(classifyApiProblem('request_timeout', 500)).toBe('timeout');
		expect(classifyApiProblem('authentication_required', 403)).toBe('authentication');
	});

	it('classifies a status-zero HTTP response as a timeout', async () => {
		const result = await parseApiError(new HttpErrorResponse({ status: 0 }), 'Unable to connect.');

		expect(result.message).toBe('Unable to connect.');
		expect(result.classification).toBe('timeout');
	});

	it('always returns a string fallback for unknown error values', async () => {
		const result = await parseApiError({ unexpected: true }, 'Something went wrong.');

		expect(result.message).toBe('Something went wrong.');
		expect(result.classification).toBe('unknown');
		expect(result.status).toBe(0);
	});
});