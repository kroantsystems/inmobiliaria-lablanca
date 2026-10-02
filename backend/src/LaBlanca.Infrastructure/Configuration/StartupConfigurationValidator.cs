using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace LaBlanca.Infrastructure.Configuration;

/// <summary>Falha cedo, na inicialização, quando a configuração é insegura ou incompleta.</summary>
public static class StartupConfigurationValidator
{
    public const int MinimumSigningKeyBytes = 32;

    private static readonly SslMode[] SecureSslModes = [SslMode.Require, SslMode.VerifyCA, SslMode.VerifyFull];

    public static IReadOnlyList<string> Validate(IConfiguration configuration, string environmentName)
    {
        var errors = new List<string>();
        var production = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionStrings:Default is not configured.");
        }
        else if (production)
        {
            var sslMode = new NpgsqlConnectionStringBuilder(connectionString).SslMode;
            if (!SecureSslModes.Contains(sslMode))
            {
                errors.Add($"ConnectionStrings:Default must use SSL Mode Require, VerifyCA or VerifyFull in Production (found {sslMode}).");
            }
        }

        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrEmpty(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyBytes)
        {
            errors.Add($"Jwt:SigningKey must have at least {MinimumSigningKeyBytes} bytes.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]) || string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
        {
            errors.Add("Jwt:Issuer and Jwt:Audience must be configured.");
        }

        if (production)
        {
            if (string.IsNullOrWhiteSpace(configuration["Site:RevalidateSecret"]))
            {
                errors.Add("Site:RevalidateSecret must be configured in Production.");
            }

            if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
            {
                errors.Add("Database:MigrateOnStartup must be false in Production; apply migrations with the EF Core bundle.");
            }
        }

        return errors;
    }
}
