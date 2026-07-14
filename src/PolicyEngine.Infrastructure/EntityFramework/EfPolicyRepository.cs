using Microsoft.EntityFrameworkCore;
using PolicyEngine.Domain.Policies;

namespace PolicyEngine.Infrastructure.EntityFramework;

public sealed class EfPolicyRepository(PolicyDbContext db) : IPolicyRepository
{
    public Task<Policy?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Policies.Include(p => p.Endorsements)
                   .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Policy>> ListAsync(PolicyStatus? status = null, CancellationToken ct = default) =>
        await db.Policies.Include(p => p.Endorsements)
                         .Where(p => status == null || p.Status == status)
                         .OrderBy(p => p.CreatedAtUtc)
                         .ToListAsync(ct);

    public async Task AddAsync(Policy policy, CancellationToken ct = default)
    {
        db.Policies.Add(policy);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Policy policy, CancellationToken ct = default)
    {
        db.Policies.Update(policy);
        await db.SaveChangesAsync(ct);
    }
}
