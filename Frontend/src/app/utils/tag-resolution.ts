import { forkJoin, map, Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Tag } from '../models/api.models';

export interface TagResolution {
	tagIds: string[];
	createdTags: Tag[];
	failedNames: string[];
}

// Splits a comma-separated manual-tag input into unique, trimmed, case-insensitive names.
export function parseManualTagNames(rawValue: string | null | undefined): string[] {
	if (!rawValue) return [];

	const unique = new Map<string, string>();
	for (const token of rawValue.split(',')) {
		const trimmed = token.trim();
		if (!trimmed) continue;

		const key = trimmed.toLowerCase();
		if (!unique.has(key)) unique.set(key, trimmed);
	}

	return [...unique.values()];
}

// Resolves selected and manual tag names to IDs, creating missing tags and reporting partial failures for retry.
export function resolveTagIds(
	selectedTagIds: string[],
	manualTagNames: string[],
	existingTags: readonly Pick<Tag, 'id' | 'name'>[],
	createTag: (name: string) => Observable<Tag>,
): Observable<TagResolution> {
	const selected = [...new Set(selectedTagIds)];
	if (manualTagNames.length === 0) {
		return of({ tagIds: selected, createdTags: [], failedNames: [] });
	}

	const existingByName = new Map(existingTags.map((tag) => [tag.name.trim().toLowerCase(), tag]));
	const existingIds = manualTagNames
		.map((name) => existingByName.get(name.toLowerCase())?.id)
		.filter((id): id is string => !!id);
	const namesToCreate = manualTagNames.filter((name) => !existingByName.has(name.toLowerCase()));

	if (namesToCreate.length === 0) {
		return of({ tagIds: [...new Set([...selected, ...existingIds])], createdTags: [], failedNames: [] });
	}

	const creations = namesToCreate.map((name) =>
		createTag(name).pipe(
			map((tag) => ({ name, tag: tag as Tag | null })),
			catchError(() => of({ name, tag: null as Tag | null })),
		),
	);

	return forkJoin(creations).pipe(
		map((results) => {
			const createdTags = results.filter((result) => result.tag).map((result) => result.tag as Tag);
			const failedNames = results.filter((result) => !result.tag).map((result) => result.name);
			const createdIds = createdTags.map((tag) => tag.id);

			return {
				tagIds: [...new Set([...selected, ...existingIds, ...createdIds])],
				createdTags,
				failedNames,
			};
		}),
	);
}

// Separates selected tag IDs that still exist from stale ones that were removed server-side.
export function pruneStaleTagIds(
	selectedTagIds: string[],
	availableTags: readonly Pick<Tag, 'id'>[],
): { keptIds: string[]; removedIds: string[] } {
	const availableIds = new Set(availableTags.map((tag) => tag.id));
	const keptIds: string[] = [];
	const removedIds: string[] = [];

	for (const id of selectedTagIds) {
		(availableIds.has(id) ? keptIds : removedIds).push(id);
	}

	return { keptIds, removedIds };
}
