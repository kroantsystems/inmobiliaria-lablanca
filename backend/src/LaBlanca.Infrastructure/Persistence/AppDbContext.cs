using Microsoft.EntityFrameworkCore;

namespace LaBlanca.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Fora do schema `public`, que a Data API do Supabase expõe com a chave anônima.
    public const string Schema = "lablanca";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
