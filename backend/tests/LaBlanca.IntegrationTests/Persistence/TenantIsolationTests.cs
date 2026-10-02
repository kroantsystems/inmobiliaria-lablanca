using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Domain.Identity;
using LaBlanca.Domain.Owners;
using LaBlanca.Infrastructure;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.Infrastructure.Persistence.Seed;
using LaBlanca.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public class TenantIsolationTests(ApiFactory factory) : IAsyncLifetime
{
    private static readonly Guid OtherTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await using var db = CreateContext(Guid.Empty);
        if (!await db.Tenants.AnyAsync(t => t.Id == OtherTenantId))
        {
            db.Tenants.Add(Tenant.Create(OtherTenantId, "Outra", "outra"));
            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Data_of_another_tenant_is_invisible()
    {
        await using (var tenantB = CreateContext(OtherTenantId))
        {
            tenantB.Owners.Add(Owner.Create("Dono B", "+595981000001", null, null, null));
            await tenantB.SaveChangesAsync();
        }

        await using var tenantA = CreateContext(SeedData.LaBlancaTenantId);
        (await tenantA.Owners.CountAsync()).Should().Be(0);

        await using var tenantBAgain = CreateContext(OtherTenantId);
        (await tenantBAgain.Owners.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task New_record_receives_tenant_from_context()
    {
        await using (var db = CreateContext(SeedData.LaBlancaTenantId))
        {
            db.Owners.Add(Owner.Create("Dono", "+595981000002", null, null, null));
            await db.SaveChangesAsync();
        }

        await using var all = CreateContext(Guid.Empty);
        var owner = await all.Owners.IgnoreQueryFilters().SingleAsync();
        owner.TenantId.Should().Be(SeedData.LaBlancaTenantId);
        owner.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Changing_tenant_of_existing_record_is_rejected()
    {
        await using var db = CreateContext(SeedData.LaBlancaTenantId);
        var owner = Owner.Create("Dono", "+595981000003", null, null, null);
        db.Owners.Add(owner);
        await db.SaveChangesAsync();

        db.Entry(owner).Property(o => o.TenantId).CurrentValue = OtherTenantId;
        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private AppDbContext CreateContext(Guid tenantId)
    {
        var tenant = new FixedTenantContext(tenantId);
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        DependencyInjection.ConfigureDbContext(builder, factory.ConnectionString)
            .AddInterceptors(new TenantAuditInterceptor(tenant, TimeProvider.System));
        return new AppDbContext((DbContextOptions<AppDbContext>)builder.Options, tenant);
    }

    private sealed class FixedTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }
}
