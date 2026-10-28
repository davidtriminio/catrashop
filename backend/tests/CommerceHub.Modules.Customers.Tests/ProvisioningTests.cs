using CommerceHub.BuildingBlocks.Persistence;
using CommerceHub.Modules.Customers.Application.Abstractions;
using CommerceHub.Modules.Customers.Application.Services;
using CommerceHub.Modules.Customers.Domain;
using Xunit;

namespace CommerceHub.Modules.Customers.Tests;

public class ProvisioningTests
{
    private sealed class FakeRepository(bool exists, bool insertWins) : ICustomerRepository
    {
        public int TryAddCalls { get; private set; }

        public Task<bool> TryAddAsync(Customer customer, CancellationToken ct = default)
        {
            TryAddCalls++;
            return Task.FromResult(insertWins);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => Task.FromResult(exists);
        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Customer?>(null);
    }

    private sealed class FakeUnitOfWork : ICustomerUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }

    private static CustomerService ServiceFor(FakeRepository repo) =>
        new(repo, new FakeUnitOfWork(), TimeProvider.System);

    [Fact]
    public async Task Existing_customer_is_not_inserted_again()
    {
        var repo = new FakeRepository(exists: true, insertWins: true);

        await ServiceFor(repo).EnsureCustomerAsync(Guid.NewGuid(), "a@b.co", "A", "B");

        Assert.Equal(0, repo.TryAddCalls);
    }

    [Fact]
    public async Task Losing_the_insert_race_is_not_an_error()
    {
        // Two requests both saw "no profile"; the other one inserted it first, so TryAddAsync reports false.
        var repo = new FakeRepository(exists: false, insertWins: false);

        var ex = await Record.ExceptionAsync(() =>
            ServiceFor(repo).EnsureCustomerAsync(Guid.NewGuid(), "a@b.co", "A", "B"));

        Assert.Null(ex);
        Assert.Equal(1, repo.TryAddCalls);
    }

    [Fact]
    public async Task New_customer_is_inserted_once()
    {
        var repo = new FakeRepository(exists: false, insertWins: true);

        await ServiceFor(repo).EnsureCustomerAsync(Guid.NewGuid(), "a@b.co", "A", "B");

        Assert.Equal(1, repo.TryAddCalls);
    }
}
