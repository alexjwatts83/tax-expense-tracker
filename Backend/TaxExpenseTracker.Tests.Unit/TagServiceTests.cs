using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class TagServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsName_AndPersists()
    {
        var repository = new InMemoryTagRepository();
        var service = new TagService(repository);

        var result = await service.CreateAsync(new CreateTagCommand("  Deductible  "));

        Assert.Equal("Deductible", result.Name);
        Assert.Single(repository.Tags);
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

        public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
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
