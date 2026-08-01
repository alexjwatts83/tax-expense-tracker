import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { finalize, take } from 'rxjs';
import {
	DataTransferExportKind,
	DataTransferImportKind,
	DataTransferImportMode,
	DataTransferImportResult,
} from '../../models/data-transfer.models';
import { DataTransferService } from '../../services/data-transfer';

interface SelectOption<T> {
	value: T;
	label: string;
}

@Component({
	selector: 'app-data-transfer',
	imports: [
		CommonModule,
		FormsModule,
		MatButtonModule,
		MatCardModule,
		MatCheckboxModule,
		MatFormFieldModule,
		MatIconModule,
		MatProgressSpinnerModule,
		MatSelectModule,
		MatTableModule,
	],
	templateUrl: './data-transfer.html',
	styleUrl: './data-transfer.scss',
})
export class DataTransfer {
	private readonly dataTransferService = inject(DataTransferService);
	private readonly cdr = inject(ChangeDetectorRef);

	readonly exportOptions: SelectOption<DataTransferExportKind>[] = [
		{ value: 'reference', label: 'Reference data backup' },
		{ value: 'trackers', label: 'Trackers' },
		{ value: 'tags', label: 'Tags' },
		{ value: 'banks', label: 'Banks' },
		{ value: 'public-holidays', label: 'Public holidays' },
		{ value: 'expenses', label: 'Expenses and tag links' },
		{ value: 'expense-tags', label: 'Expense tag links' },
		{ value: 'work-locations', label: 'Work locations' },
		{ value: 'leave', label: 'Leave entries' },
	];

	readonly importOptions: SelectOption<DataTransferImportKind>[] = [
		{ value: 'reference', label: 'Reference data backup' },
		{ value: 'expenses', label: 'Expenses and tag links' },
		{ value: 'work-locations', label: 'Work locations' },
		{ value: 'leave', label: 'Leave entries' },
	];

	readonly modeOptions: SelectOption<DataTransferImportMode>[] = [
		{ value: 'Upsert', label: 'Upsert' },
		{ value: 'InsertOnly', label: 'Insert only' },
		{ value: 'Replace', label: 'Replace' },
	];

	readonly displayedColumns = ['entity', 'received', 'created', 'updated', 'skipped', 'warnings', 'errors'];

	exportKind: DataTransferExportKind = 'reference';
	includeSoftDeleted = true;
	importKind: DataTransferImportKind = 'reference';
	importMode: DataTransferImportMode = 'Upsert';
	allowDeletes = false;
	dryRun = true;
	selectedFile: File | null = null;
	parsedPayload: unknown = null;
	result: DataTransferImportResult | null = null;
	isExporting = false;
	isImporting = false;
	validationPassed = false;
	errorMessage = '';
	infoMessage = '';

	downloadExport(): void {
		if (this.isExporting)
			return;

		this.isExporting = true;
		this.errorMessage = '';
		this.infoMessage = '';

		this.dataTransferService
			.export(this.exportKind, this.includeSoftDeleted)
			.pipe(
				take(1),
				finalize(() => {
					this.isExporting = false;
					this.cdr.detectChanges();
				}),
			)
			.subscribe({
				next: (blob) => {
					this.saveBlob(blob, `tax-expense-tracker-${this.exportKind}-${this.dateStamp()}.json`);
					this.infoMessage = 'Export downloaded.';
				},
				error: (error) => {
					this.errorMessage = this.resolveError(error, 'Unable to export data.');
				},
			});
	}

	async selectFile(event: Event): Promise<void> {
		const input = event.target as HTMLInputElement;
		const file = input.files?.[0] ?? null;
		this.resetValidation();
		this.selectedFile = file;

		if (!file)
			return;

		try {
			this.parsedPayload = JSON.parse(await file.text()) as unknown;
			this.infoMessage = `${file.name} is ready for validation.`;
		} catch {
			this.parsedPayload = null;
			this.errorMessage = 'The selected file is not valid JSON.';
		} finally {
			this.cdr.detectChanges();
		}
	}

	runImport(): void {
		if (this.isImporting || this.parsedPayload === null)
			return;

		if (!this.dryRun && !this.validationPassed)
			return;

		this.isImporting = true;
		this.errorMessage = '';
		this.infoMessage = '';
		this.result = null;

		this.dataTransferService
			.import(
				this.importKind,
				this.parsedPayload,
				this.importMode,
				this.dryRun,
				this.importMode === 'Replace' && this.allowDeletes,
			)
			.pipe(
				take(1),
				finalize(() => {
					this.isImporting = false;
					this.cdr.detectChanges();
				}),
			)
			.subscribe({
				next: (result) => {
					this.result = result;
					const hasErrors = result.results.some((item) => item.errors.length > 0);
					if (result.dryRun) {
						this.validationPassed = !hasErrors;
						this.infoMessage = hasErrors
							? 'Validation completed with errors.'
							: 'Validation passed. Turn off dry run to enable import.';
					} else {
						this.validationPassed = false;
						this.infoMessage = hasErrors ? 'Import was rolled back.' : 'Import completed.';
					}
				},
				error: (error) => {
					this.validationPassed = false;
					this.errorMessage = this.resolveError(error, 'Unable to process the import.');
				},
			});
	}

	settingsChanged(): void {
		this.validationPassed = false;
		this.dryRun = true;
		this.result = null;
		this.infoMessage = this.selectedFile ? 'Import settings changed. Run validation again.' : '';
		if (this.importMode !== 'Replace')
			this.allowDeletes = false;
	}

	toggleDryRun(dryRun: boolean): void {
		if (!dryRun && !this.validationPassed)
			return;

		this.dryRun = dryRun;
	}

	private resetValidation(): void {
		this.parsedPayload = null;
		this.result = null;
		this.validationPassed = false;
		this.dryRun = true;
		this.errorMessage = '';
		this.infoMessage = '';
	}

	private saveBlob(blob: Blob, filename: string): void {
		const url = URL.createObjectURL(blob);
		const anchor = document.createElement('a');
		anchor.href = url;
		anchor.download = filename;
		anchor.click();
		URL.revokeObjectURL(url);
	}

	private dateStamp(): string {
		return new Date().toISOString().slice(0, 10);
	}

	private resolveError(error: any, fallback: string): string {
		return error?.error?.detail ?? error?.error?.title ?? error?.error ?? fallback;
	}
}