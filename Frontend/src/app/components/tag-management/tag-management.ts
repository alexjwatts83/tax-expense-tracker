import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
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

  readonly displayedColumns: string[] = ['name', 'createdAt', 'actions'];

  readonly tagForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
  });

  tags: Tag[] = [];
  lastDeletedTag: Tag | null = null;
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  infoMessage = '';

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

    this.isSubmitting = true;
    this.errorMessage = '';
    this.infoMessage = '';

    this.tagService.create({ name }).subscribe({
      next: (created) => {
        this.tags = [created, ...this.tags];
        this.tagForm.reset();
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

  softDeleteTag(id: string): void {
    this.errorMessage = '';
    this.infoMessage = '';

    const tag = this.tags.find((x) => x.id === id);
    if (!tag) {
      return;
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
}
