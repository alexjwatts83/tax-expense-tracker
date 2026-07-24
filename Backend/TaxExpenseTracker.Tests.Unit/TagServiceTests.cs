using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class TagServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsName_AndPersists()
    {
        var repository = new InMemoryTagRepository();
        var service = new TagService(repository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateTagCommand("  Deductible  "));

        Assert.Equal("Deductible", result.Name);
        Assert.Equal(Tag.DefaultColor, result.Color);
        Assert.Single(repository.Tags);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenTagMissing()
    {
        var repository = new InMemoryTagRepository();
        var service = new TagService(repository, TestTime.TimeProvider);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateTagCommand("Updated"));

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenTagMissing()
    {
        var repository = new InMemoryTagRepository();
        var service = new TagService(repository, TestTime.TimeProvider);

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_RestoresTag_WhenSoftDeleted()
    {
        var repository = new InMemoryTagRepository();
        var tag = Tag.Create("Equipment", null, TestTime.TimeProvider);
        tag.SoftDelete(TestTime.TimeProvider);
        repository.Tags.Add(tag);

        var service = new TagService(repository, TestTime.TimeProvider);

        var result = await service.RestoreAsync(tag.Id);

        Assert.True(result);
        Assert.False(tag.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTag_WhenExists()
    {
        var repository = new InMemoryTagRepository();
        var tag = Tag.Create("Old", null, TestTime.TimeProvider);
        repository.Tags.Add(tag);

        var service = new TagService(repository, TestTime.TimeProvider);

        var result = await service.UpdateAsync(tag.Id, new UpdateTagCommand("  New  ", "#112233"));

        Assert.True(result);
        Assert.Equal("New", tag.Name);
        Assert.Equal("#112233", tag.Color);
        Assert.True(repository.SaveChangesCalled);
    }

    private sealed class InMemoryTagRepository : ITagRepository
    {
        public List<Tag> Tags { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Tag>>(Tags.ToList());
        }

        public Task<IReadOnlyList<Tag>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Tag>>(Tags.ToList());
        }

        public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tags.FirstOrDefault(x => x.Id == id));
        }

        public Task<Tag?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tags.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            Tags.Add(tag);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
