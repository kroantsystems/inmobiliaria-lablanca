using LaBlanca.Infrastructure.Persistence;
using LaBlanca.Infrastructure.Persistence.Seed;
using LaBlanca.IntegrationTests.Infrastructure;
using Npgsql;

namespace LaBlanca.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public class SupabaseSecurityTests(ApiFactory factory)
{
    [Fact]
    public async Task Application_tables_live_outside_public_schema()
    {
        await using var connection = await OpenAsync();

        var publicTables = await ScalarAsync<long>(connection,
            "SELECT count(*) FROM pg_tables WHERE schemaname = 'public'");
        var appTables = await ScalarAsync<long>(connection,
            $"SELECT count(*) FROM pg_tables WHERE schemaname = '{AppDbContext.Schema}'");

        publicTables.Should().Be(0);
        appTables.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task Every_table_in_application_schema_has_row_level_security()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT c.relname FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace " +
            $"WHERE n.nspname = '{AppDbContext.Schema}' AND c.relkind = 'r' AND NOT c.relrowsecurity",
            connection);

        var withoutRls = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                withoutRls.Add(reader.GetString(0));
            }
        }

        withoutRls.Should().BeEmpty();
    }

    [Fact]
    public async Task Anonymous_role_reads_no_rows_even_with_select_grant()
    {
        await using var connection = await OpenAsync();
        await ExecuteAsync(connection, """
            DO $$ BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN CREATE ROLE anon NOLOGIN; END IF;
            END $$;
            GRANT USAGE ON SCHEMA lablanca TO anon;
            GRANT SELECT ON ALL TABLES IN SCHEMA lablanca TO anon;
            """);

        var asOwner = await ScalarAsync<long>(connection, "SELECT count(*) FROM lablanca.\"Zones\"");
        await ExecuteAsync(connection, "SET ROLE anon");
        var asAnon = await ScalarAsync<long>(connection, "SELECT count(*) FROM lablanca.\"Zones\"");
        await ExecuteAsync(connection, "RESET ROLE");

        asOwner.Should().BeGreaterThan(0, "seeded zones exist");
        asAnon.Should().Be(0);
    }

    [Fact]
    public async Task Seed_creates_la_blanca_tenant_settings_and_zones()
    {
        await using var connection = await OpenAsync();

        (await ScalarAsync<long>(connection, $"SELECT count(*) FROM lablanca.\"Tenants\" WHERE \"Id\" = '{SeedData.LaBlancaTenantId}'")).Should().Be(1);
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM lablanca.\"SiteSettings\"")).Should().Be(1);
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM lablanca.\"Zones\"")).Should().Be(5);
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM lablanca.\"ZoneTranslations\"")).Should().Be(20);
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(factory.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
