export type DataTransferImportMode = 'InsertOnly' | 'Upsert' | 'Replace';

export type DataTransferImportKind = 'reference' | 'expenses' | 'work-locations' | 'leave';

export type DataTransferExportKind =
	| 'reference'
	| 'trackers'
	| 'tags'
	| 'banks'
	| 'public-holidays'
	| 'expenses'
	| 'expense-tags'
	| 'work-locations'
	| 'leave';

export interface DataTransferEntityImportResult {
	entity: string;
	receivedCount: number;
	createdCount: number;
	updatedCount: number;
	skippedCount: number;
	warnings: string[];
	warningCodes: string[];
	errors: string[];
	errorCodes: string[];
}

export interface DataTransferImportResult {
	dryRun: boolean;
	mode: number;
	results: DataTransferEntityImportResult[];
	correlationId?: string;
}