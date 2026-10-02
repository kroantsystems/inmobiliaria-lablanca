using LaBlanca.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LaBlanca.Infrastructure.Tenancy;

/// <summary>Admin autenticado: tenant do token. Visitante (ou sem requisição): tenant do site em <c>Site:TenantId</c>.</summary>
internal sealed class HttpTenantContext(IHttpContextAccessor accessor, IConfiguration configuration) : ITenantContext
{
    public const string ClaimType = "tenant_id";

    public Guid TenantId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return Guid.TryParse(user.FindFirst(ClaimType)?.Value, out var fromToken) ? fromToken : Guid.Empty;
            }

            return Guid.TryParse(configuration["Site:TenantId"], out var site) ? site : Guid.Empty;
        }
    }
}
