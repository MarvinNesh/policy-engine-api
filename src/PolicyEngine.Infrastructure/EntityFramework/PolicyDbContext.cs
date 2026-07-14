using Microsoft.EntityFrameworkCore;
using PolicyEngine.Domain.Common;
using PolicyEngine.Domain.Policies;

namespace PolicyEngine.Infrastructure.EntityFramework;

public sealed class PolicyDbContext(DbContextOptions<PolicyDbContext> options) : DbContext(options)
{
    public DbSet<Policy> Policies => Set<Policy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Policy>(policy =>
        {
            policy.HasKey(p => p.Id);
            policy.Property(p => p.PolicyNumber).HasMaxLength(32);
            policy.Property(p => p.HolderName).HasMaxLength(120).IsRequired();
            policy.HasIndex(p => p.PolicyNumber);

            // Value objects stored as complex types (flattened columns).
            policy.ComplexProperty(p => p.Term);
            policy.ComplexProperty(p => p.SumInsured);
            policy.ComplexProperty(p => p.AnnualPremium);

            // Nullable value objects can't be complex types in EF Core 8,
            // so RefundDue is stored via a simple conversion (amount + fixed ZAR).
            policy.Property(p => p.RefundDue)
                  .HasConversion(
                      m => m == null ? (decimal?)null : m.Value.Amount,
                      a => a == null ? null : Money.Zar(a.Value));

            policy.OwnsMany(p => p.Endorsements, e =>
            {
                e.WithOwner().HasForeignKey("PolicyId");
                e.HasKey(x => x.Id);

                // Complex types are not supported inside owned types in EF Core 8,
                // so endorsement amounts use value conversions instead.
                e.Property(x => x.PreviousSumInsured)
                 .HasConversion(m => m.Amount, a => Money.Zar(a));
                e.Property(x => x.NewSumInsured)
                 .HasConversion(m => m.Amount, a => Money.Zar(a));
                e.Property(x => x.PremiumDelta)
                 .HasConversion(m => m.Amount, a => Money.Zar(a));
            });

            policy.Navigation(p => p.Endorsements)
                  .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}