using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Tags;

public sealed class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IReadOnlyList<TagReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _tagRepository.GetAllAsync(cancellationToken);

        return tags
            .OrderBy(t => t.Name)
            .Select(t => new TagReadDto(t.Id, t.Name, t.CreatedAt))
            .ToList();
    }

    public async Task<TagReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);

        return tag is null
            ? null
            : new TagReadDto(tag.Id, tag.Name, tag.CreatedAt);
    }

    public async Task<TagReadDto> CreateAsync(CreateTagCommand command, CancellationToken cancellationToken = default)
    {
        var tag = Tag.Create(command.Name);

        await _tagRepository.AddAsync(tag, cancellationToken);
        await _tagRepository.SaveChangesAsync(cancellationToken);

        return new TagReadDto(tag.Id, tag.Name, tag.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTagCommand command, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag is null)
        {
            return false;
        }

        tag.Rename(command.Name);
        await _tagRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag is null)
        {
            return false;
        }

        tag.SoftDelete();
        await _tagRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (tag is null || !tag.IsDeleted)
        {
            return false;
        }

        tag.Restore();
        await _tagRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
