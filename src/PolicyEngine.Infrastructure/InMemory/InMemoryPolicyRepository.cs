using System.Collections.Concurrent;
using PolicyEngine.Domain.Policies;

namespace PolicyEngine.Infrastructure.InMemory;

/// <summary>
/// Thread-safe in-memory repository. Used for local development without a
/// database ("Persistence": "InMemory") and as a fast test double in unit tests.
/// </summary>
public sealed class InMemoryPolicyRepository : IPolicyRepository
{
    private readonly ConcurrentDictionary<Guid, Policy> _store = new();

    public Task<Policy?> GetAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(id, out var policy) ? policy : null);

    public Task<IReadOnlyList<Policy>> ListAsync(PolicyStatus? status = null, CancellationToken ct = default)
    {
        IReadOnlyList<Policy> result = _store.Values
            .Where(p => status is null || p.Status == status)
            .OrderBy(p => p.CreatedAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(Policy policy, CancellationToken ct = default)
    {
        _store[policy.Id] = policy;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Policy policy, CancellationToken ct = default)
    {
        _store[policy.Id] = policy;
        return Task.CompletedTask;
    }
}
