import { HttpErrorResponse } from '@angular/common/http';
import { ApiProblem, ApiProblemClassification, ParsedApiError } from '../models/api-problem.models';

const correlationHeader = 'X-Correlation-ID';

export async function parseApiError(error: unknown, fallbackMessage: string): Promise<ParsedApiError> {
	const httpError = error instanceof HttpErrorResponse ? error : null;
	const body = httpError?.error ?? error;
	const bodyText = await readBodyText(body);
	const problem = bodyText === null ? toApiProblem(body) : parseProblemText(bodyText);
	const status = positiveNumber(httpError?.status) ?? positiveNumber(problem?.status) ?? 0;
	const code = problem?.code ?? null;

	return {
		problem,
		message: problem?.detail ?? problem?.title ?? plainTextMessage(bodyText) ?? fallbackMessage,
		classification: httpError ? classifyApiProblem(code, status) : 'unknown',
		status,
		code,
		correlationId:
			problem?.correlationId ?? cleanString(httpError?.headers?.get(correlationHeader)) ?? null,
		errors: problem?.errors ?? null,
	};
}

export function classifyApiProblem(code: string | null, status: number): ApiProblemClassification {
	switch (code) {
		case 'validation_error':
			return 'validation';
		case 'invalid_reference':
			return 'invalid-reference';
		case 'not_found':
			return 'not-found';
		case 'conflict':
			return 'conflict';
		case 'authentication_required':
			return 'authentication';
		case 'access_denied':
			return 'authorization';
		case 'request_timeout':
			return 'timeout';
		case 'server_error':
			return 'server';
	}

	switch (status) {
		case 400:
			return 'validation';
		case 401:
			return 'authentication';
		case 403:
			return 'authorization';
		case 404:
			return 'not-found';
		case 408:
			return 'timeout';
		case 409:
			return 'conflict';
		default:
			if (status >= 500)
				return 'server';

			return status === 0 ? 'timeout' : 'unknown';
	}
}

async function readBodyText(body: unknown): Promise<string | null> {
	if (body instanceof Blob)
		return body.text();

	if (typeof body === 'string')
		return body;

	return null;
}

function parseProblemText(text: string): ApiProblem | null {
	const trimmed = text.trim();
	if (!trimmed.startsWith('{'))
		return null;

	try {
		return toApiProblem(JSON.parse(trimmed) as unknown);
	} catch {
		return null;
	}
}

function toApiProblem(value: unknown): ApiProblem | null {
	if (!isRecord(value))
		return null;

	const problem: ApiProblem = {};
	assignString(problem, 'type', value['type']);
	assignString(problem, 'title', value['title']);
	assignNumber(problem, 'status', value['status']);
	assignString(problem, 'detail', value['detail']);
	assignString(problem, 'instance', value['instance']);
	assignString(problem, 'code', value['code']);
	assignString(problem, 'correlationId', value['correlationId']);

	const errors = parseFieldErrors(value['errors']);
	if (errors)
		problem.errors = errors;

	return Object.keys(problem).length > 0 ? problem : null;
}

function plainTextMessage(bodyText: string | null): string | null {
	if (bodyText === null)
		return null;

	const trimmed = bodyText.trim();
	return trimmed && !trimmed.startsWith('{') ? trimmed : null;
}

function parseFieldErrors(value: unknown): Record<string, string[]> | null {
	if (!isRecord(value))
		return null;

	const errors: Record<string, string[]> = {};
	for (const [field, messages] of Object.entries(value)) {
		if (!Array.isArray(messages))
			continue;

		const validMessages = messages.map(cleanString).filter((message): message is string => message !== null);
		if (validMessages.length > 0)
			errors[field] = validMessages;
	}

	return Object.keys(errors).length > 0 ? errors : null;
}

function assignString(problem: ApiProblem, key: keyof ApiProblem, value: unknown): void {
	const stringValue = cleanString(value);
	if (stringValue)
		Object.assign(problem, { [key]: stringValue });
}

function assignNumber(problem: ApiProblem, key: keyof ApiProblem, value: unknown): void {
	const numberValue = positiveNumber(value);
	if (numberValue)
		Object.assign(problem, { [key]: numberValue });
}

function cleanString(value: unknown): string | null {
	if (typeof value !== 'string')
		return null;

	return value.trim() || null;
}

function positiveNumber(value: unknown): number | null {
	return typeof value === 'number' && Number.isFinite(value) && value > 0 ? value : null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
	return typeof value === 'object' && value !== null && !Array.isArray(value);
}