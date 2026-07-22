import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
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

  readonly displayedColumns: string[] = ['name', 'createdAt', 'actions'];

  readonly tagForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
  });

  tags: Tag[] = [];
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadTags();
  }

  loadTags(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.tagService.getAll().subscribe({
      next: (tags) => {
        this.tags = tags;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load tags. Ensure the API is running.';
        this.isLoading = false;
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

    this.tagService.create({ name }).subscribe({
      next: (created) => {
        this.tags = [created, ...this.tags];
        this.tagForm.reset();
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

    this.tagService.softDelete(id).subscribe({
      next: () => {
        this.tags = this.tags.filter((tag) => tag.id !== id);
      },
      error: () => {
        this.errorMessage = 'Unable to delete tag.';
      },
    });
  }
}
