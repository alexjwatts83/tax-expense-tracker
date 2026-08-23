export interface ApiProblem {
	type?: string;
	title?: string;
	status?: number;
	detail?: string;
	instance?: string;
	code?: string;
	correlationId?: string;
	errors?: Record<string, string[]>;
}

export type ApiProblemClassification =
	| 'validation'
	| 'invalid-reference'
	| 'not-found'
	| 'conflict'
	| 'authentication'
	| 'authorization'
	| 'timeout'
	| 'server'
	| 'unknown';

export interface ParsedApiError {
	problem: ApiProblem | null;
	message: string;
	classification: ApiProblemClassification;
	status: number;
	code: string | null;
	correlationId: string | null;
	errors: Record<string, string[]> | null;
}