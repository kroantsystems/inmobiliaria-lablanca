using System.Security.Claims;
using LaBlanca.Application.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LaBlanca.IntegrationTests.Infrastructure;

public static class TestSettings
{
    public const string JwtIssuer = "lablanca-api";
    public const string JwtAudience = "lablanca-site";
    public const string JwtSigningKey = "integration-tests-signing-key-0123456789abcdef";
    public const string FrontendOrigin = "http://localhost:3000";

    public static IWebHostBuilder ApplyDefaults(this IWebHostBuilder builder, string connectionString, string storageRoot)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Jwt:SigningKey", JwtSigningKey);
        builder.UseSetting("Cors:AllowedOrigins:0", FrontendOrigin);
        builder.UseSetting("Storage:RootPath", storageRoot);
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        // Limites altos para os testes não interferirem entre si; testes de limite usam valores próprios.
        builder.UseSetting("RateLimiting:Login:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:PublicForms:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:Analytics:PermitLimit", "10000");
        return builder;
    }
}

public static class TestTokens
{
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static string Create(IEnumerable<string>? permissions = null, Guid? tenantId = null, DateTime? expires = null)
    {
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("jti", Guid.NewGuid().ToString()),
            new("name", "Test Admin"),
            new("email", "admin@test.local"),
            new("tenant_id", (tenantId ?? TenantId).ToString()),
            new("role", Roles.Admin),
        };
        claims.AddRange((permissions ?? []).Select(p => new Claim(Permissions.ClaimType, p)));

        var expiresAt = expires ?? DateTime.UtcNow.AddMinutes(15);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = TestSettings.JwtIssuer,
            Audience = TestSettings.JwtAudience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = expiresAt.AddMinutes(-30),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSettings.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256),
        });
    }
}
