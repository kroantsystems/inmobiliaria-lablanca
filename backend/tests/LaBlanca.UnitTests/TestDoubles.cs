using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LaBlanca.UnitTests;

internal sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

internal sealed class FixedTenantContext(Guid tenantId) : ITenantContext
{
    public Guid TenantId { get; } = tenantId;
}

internal static class InMemoryDb
{
    public static readonly Guid TenantId = Guid.Parse("6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71");

    public static AppDbContext Create(Guid? tenantId = null, string? name = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name ?? Guid.NewGuid().ToString()).Options,
            new FixedTenantContext(tenantId ?? TenantId));
}
