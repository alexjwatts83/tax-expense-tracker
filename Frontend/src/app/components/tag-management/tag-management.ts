import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { finalize, take } from 'rxjs';
import { Tag } from '../../models/api.models';
import { TagService } from '../../services/tag';

@Component({
  selector: 'app-tag-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './tag-management.html',
  styleUrl: './tag-management.scss',
})
export class TagManagement implements OnInit {
  private readonly tagService = inject(TagService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns: string[] = ['name', 'color', 'createdAt', 'actions'];

  readonly tagForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    color: ['#CBD5E1', [Validators.required]],
  });

  tags: Tag[] = [];
  lastDeletedTag: Tag | null = null;
  editingTagId: string | null = null;
  savingTagId: string | null = null;
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  infoMessage = '';

  readonly editTagForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    color: ['#CBD5E1', [Validators.required]],
  });

  ngOnInit(): void {
    this.loadTags();
  }

  loadTags(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.tagService
      .getAll()
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (tags) => {
          this.tags = tags;
        },
        error: () => {
          this.errorMessage = 'Unable to load tags. Ensure the API is running.';
        },
      });
  }

  createTag(): void {
    if (this.tagForm.invalid || this.isSubmitting) {
      this.tagForm.markAllAsTouched();
      return;
    }

    const name = this.tagForm.value.name?.trim() ?? '';
    const color = this.tagForm.value.color ?? '#CBD5E1';

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.tagService.create({ name, color }).subscribe({
      next: (created) => {
        this.tags = [created, ...this.tags];
        this.tagForm.reset({
          name: '',
          color: '#CBD5E1',
        });
        this.lastDeletedTag = null;
        this.infoMessage = 'Tag created.';
        this.isSubmitting = false;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to create tag.';
        this.isSubmitting = false;
      },
    });
  }

  startEditTag(tag: Tag): void {
    this.editingTagId = tag.id;
    this.editTagForm.reset({
      name: tag.name,
      color: this.normalizeHexColor(tag.color),
    });
    this.errorMessage = '';
    this.infoMessage = '';
  }

  cancelEditTag(): void {
    this.editingTagId = null;
    this.savingTagId = null;
    this.editTagForm.reset({
      name: '',
      color: '#CBD5E1',
    });
  }

  saveEditedTag(tag: Tag): void {
    if (this.editTagForm.invalid || this.savingTagId) {
      this.editTagForm.markAllAsTouched();
      return;
    }

    const name = this.editTagForm.value.name?.trim() ?? '';
    const color = this.normalizeHexColor(this.editTagForm.value.color);

    this.savingTagId = tag.id;
    this.errorMessage = '';
    this.infoMessage = '';

    this.tagService.update(tag.id, { name, color }).subscribe({
      next: (updated) => {
        this.tags = this.tags.map((x) => (x.id === updated.id ? updated : x));
        this.cancelEditTag();
        this.infoMessage = `Tag "${updated.name}" updated.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to update tag.';
        this.savingTagId = null;
      },
    });
  }

  isEditingTag(tagId: string): boolean {
    return this.editingTagId === tagId;
  }

  softDeleteTag(id: string): void {
    this.errorMessage = '';
    this.infoMessage = '';

    const tag = this.tags.find((x) => x.id === id);
    if (!tag) {
      return;
    }

    if (this.editingTagId === id) {
      this.cancelEditTag();
    }

    this.tagService.softDelete(id).subscribe({
      next: () => {
        this.lastDeletedTag = tag;
        this.tags = this.tags.filter((tag) => tag.id !== id);
        this.infoMessage = `Tag "${tag.name}" deleted.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to delete tag.';
      },
    });
  }

  undoDeleteTag(): void {
    if (!this.lastDeletedTag) {
      return;
    }

    const tag = this.lastDeletedTag;
    this.errorMessage = '';
    this.infoMessage = '';

    this.tagService.restore(tag.id).subscribe({
      next: () => {
        this.tags = [tag, ...this.tags].sort((a, b) => a.name.localeCompare(b.name));
        this.lastDeletedTag = null;
        this.infoMessage = `Tag "${tag.name}" restored.`;
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail ?? err?.error ?? 'Unable to restore tag.';
      },
    });
  }

  getTagChipStyles(color: string | null | undefined): Record<string, string> {
    const resolvedColor = this.normalizeHexColor(color);

    return {
      'background-color': resolvedColor,
      color: '#FFFFFF',
      '--mat-chip-label-text-color': '#FFFFFF',
      '--mdc-chip-label-text-color': '#FFFFFF',
      '--mdc-chip-elevated-label-text-color': '#FFFFFF',
      '--mdc-chip-outline-label-text-color': '#FFFFFF',
      border: '1px solid rgb(0 0 0 / 10%)',
    };
  }

  private normalizeHexColor(value: string | null | undefined): string {
    if (!value) {
      return '#CBD5E1';
    }

    const trimmed = value.trim();
    if (!/^#[0-9A-Fa-f]{6}$/.test(trimmed)) {
      return '#CBD5E1';
    }

    return trimmed;
  }

}
