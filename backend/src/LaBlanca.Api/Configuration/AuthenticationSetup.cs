using LaBlanca.Application.Authorization;
using LaBlanca.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LaBlanca.Api.Configuration;

public static class AuthenticationSetup
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = jwt.Value.CreateValidationParameters();
            });

        services.AddAuthorization(options =>
        {
            // Seguro por padrão: todo endpoint exige login, salvo [AllowAnonymous] explícito.
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(Permissions.ClaimType, permission));
            }
        });

        return services;
    }
}
