using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Tags;

public sealed class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly TimeProvider _timeProvider;

    public TagService(ITagRepository tagRepository, TimeProvider timeProvider)
    {
        _tagRepository = tagRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<TagReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _tagRepository.GetAllAsync(cancellationToken);

        return tags
            .OrderBy(t => t.Name)
            .Select(t => new TagReadDto(t.Id, t.Name, t.Color, t.CreatedAt))
            .ToList();
    }

    public async Task<TagReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);

        return tag is null
            ? null
            : new TagReadDto(tag.Id, tag.Name, tag.Color, tag.CreatedAt);
    }

    public async Task<TagReadDto> CreateAsync(CreateTagCommand command, CancellationToken cancellationToken = default)
    {
        var tag = Tag.Create(command.Name, command.Color, _timeProvider);

        await _tagRepository.AddAsync(tag, cancellationToken);
        await _tagRepository.SaveChangesAsync(cancellationToken);

        return new TagReadDto(tag.Id, tag.Name, tag.Color, tag.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTagCommand command, CancellationToken cancellationToken = default)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag is null)
        {
            return false;
        }

        tag.Rename(command.Name);
        tag.SetColor(command.Color);
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

        tag.SoftDelete(_timeProvider);
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

        tag.Restore(_timeProvider);
        await _tagRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
