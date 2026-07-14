using Microsoft.EntityFrameworkCore;
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

            policy.ComplexProperty(p => p.Term);
            policy.ComplexProperty(p => p.SumInsured);
            policy.ComplexProperty(p => p.AnnualPremium);

            // Nullable value objects are stored as flattened owned columns via
            // a simple conversion: RefundDue amount + fixed currency.
            policy.Property(p => p.RefundDue)
                  .HasConversion(
                      m => m == null ? (decimal?)null : m.Value.Amount,
                      a => a == null ? null : Domain.Common.Money.Zar(a.Value));

            policy.OwnsMany(p => p.Endorsements, e =>
            {
                e.WithOwner().HasForeignKey("PolicyId");
                e.HasKey(x => x.Id);
                e.ComplexProperty(x => x.PreviousSumInsured);
                e.ComplexProperty(x => x.NewSumInsured);
                e.ComplexProperty(x => x.PremiumDelta);
            });

            policy.Navigation(p => p.Endorsements)
                  .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
