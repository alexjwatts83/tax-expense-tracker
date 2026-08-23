import { firstValueFrom, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { Tag } from '../models/api.models';
import { parseManualTagNames, pruneStaleTagIds, resolveTagIds } from './tag-resolution';

function tag(id: string, name: string): Tag {
	return { id, name, color: '#123456' } as Tag;
}

describe('parseManualTagNames', () => {
	it('trims, drops blanks, and de-duplicates case-insensitively', () => {
		expect(parseManualTagNames(' Office , office ,,  Travel ')).toEqual(['Office', 'Travel']);
	});

	it('returns an empty array for blank input', () => {
		expect(parseManualTagNames('')).toEqual([]);
		expect(parseManualTagNames(null)).toEqual([]);
	});
});

describe('resolveTagIds', () => {
	it('returns selected IDs when there are no manual names', async () => {
		const result = await firstValueFrom(resolveTagIds(['a', 'a', 'b'], [], [], () => of(tag('x', 'x'))));

		expect(result.tagIds).toEqual(['a', 'b']);
		expect(result.createdTags).toEqual([]);
	});

	it('reuses existing tags by name without creating', async () => {
		const create = vi.fn(() => of(tag('new', 'new')));

		const result = await firstValueFrom(resolveTagIds([], ['Office'], [tag('t1', 'office')], create));

		expect(result.tagIds).toEqual(['t1']);
		expect(create).not.toHaveBeenCalled();
	});

	it('creates missing tags and merges IDs', async () => {
		const result = await firstValueFrom(
			resolveTagIds(['sel'], ['Travel'], [], (name) => of(tag('created', name))),
		);

		expect(result.tagIds).toEqual(['sel', 'created']);
		expect(result.createdTags.map((t) => t.id)).toEqual(['created']);
		expect(result.failedNames).toEqual([]);
	});

	it('reports partial failures while keeping successes for retry', async () => {
		const result = await firstValueFrom(
			resolveTagIds([], ['Good', 'Bad'], [], (name) =>
				name === 'Bad' ? throwError(() => new Error('boom')) : of(tag('good-id', name)),
			),
		);

		expect(result.tagIds).toEqual(['good-id']);
		expect(result.createdTags.map((t) => t.name)).toEqual(['Good']);
		expect(result.failedNames).toEqual(['Bad']);
	});
});

describe('pruneStaleTagIds', () => {
	it('separates existing selections from stale ones', () => {
		const result = pruneStaleTagIds(['keep', 'stale'], [tag('keep', 'keep')]);

		expect(result.keptIds).toEqual(['keep']);
		expect(result.removedIds).toEqual(['stale']);
	});
});
