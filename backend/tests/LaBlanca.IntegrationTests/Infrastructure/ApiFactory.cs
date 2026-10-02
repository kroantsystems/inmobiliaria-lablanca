using FluentValidation;
using LaBlanca.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace LaBlanca.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Mesma versão major do PostgreSQL do projeto Supabase e do docker-compose.
    public const string PostgresImage = "postgres:17-alpine";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase("lablanca")
        .Build();

    private Respawner? _respawner;

    public string ConnectionString => _database.GetConnectionString();

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "lablanca-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ApplyDefaults(ConnectionString, StorageRoot);
        builder.ConfigureTestServices(AddTestEndpoints);
    }

    public static void AddTestEndpoints(IServiceCollection services)
    {
        var assembly = typeof(ApiFactory).Assembly;
        services.AddControllers().AddApplicationPart(assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
    }

    public HttpClient CreateClientWithToken(params string[] permissions)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestTokens.Create(permissions));
        return client;
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = [AppDbContext.Schema],
            // Tabelas com seed: testes que as alteram devem restaurar o estado (ou criar registros próprios).
            TablesToIgnore = ["__EFMigrationsHistory", "Tenants", "Zones", "ZoneTranslations", "SiteSettings"],
        });

        await _respawner.ResetAsync(connection);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
        if (Directory.Exists(StorageRoot))
        {
            Directory.Delete(StorageRoot, recursive: true);
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
