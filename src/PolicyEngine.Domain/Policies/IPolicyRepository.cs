namespace PolicyEngine.Domain.Policies;

/// <summary>Persistence abstraction for the Policy aggregate.</summary>
public interface IPolicyRepository
{
    Task<Policy?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Policy>> ListAsync(PolicyStatus? status = null, CancellationToken ct = default);
    Task AddAsync(Policy policy, CancellationToken ct = default);
    Task UpdateAsync(Policy policy, CancellationToken ct = default);
}
