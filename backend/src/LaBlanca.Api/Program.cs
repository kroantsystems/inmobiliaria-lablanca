using System.Net;
using System.Text.Json.Serialization;
using LaBlanca.Api.Configuration;
using LaBlanca.Application;
using LaBlanca.Domain.Common;
using LaBlanca.Infrastructure;
using LaBlanca.Infrastructure.Configuration;
using LaBlanca.Infrastructure.Persistence;
using LaBlanca.Shared;
using LaBlanca.Shared.Localization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

var configurationErrors = StartupConfigurationValidator.Validate(builder.Configuration, builder.Environment.EnvironmentName);
if (configurationErrors.Count > 0)
{
    throw new InvalidOperationException("Invalid configuration:" + Environment.NewLine + string.Join(Environment.NewLine, configurationErrors));
}

builder.Host.UseSerilog((context, services, logger) =>
{
    logger.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var filePath = context.Configuration["Logging:File:Path"];
    if (!string.IsNullOrWhiteSpace(filePath))
    {
        logger.WriteTo.File(filePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
    }
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddApiProblemDetails(o => o.Map<DomainException>(StatusCodes.Status400BadRequest));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApiAuthentication();
builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = SupportedCultures.All;
    options.DefaultRequestCulture = new RequestCulture(SupportedCultures.Default);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.FallBackToParentCultures = true;
    options.FallBackToParentUICultures = true;
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in knownProxies)
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }
});

var app = builder.Build();

if (knownProxies.Length > 0)
{
    app.UseForwardedHeaders();
}

app.Use((context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    return next(context);
});

// Antes do tratamento de erros: a cultura definida aqui precisa valer para as respostas de erro.
app.UseRequestLocalization();
// Fora do tratamento de erros para registrar o status final (ex.: 404), não um 500 da exceção ainda não tratada.
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapApiHealthChecks();

// Rotas inexistentes respondem 404 (e não 401 pela política padrão de autorização).
app.MapFallback(() => Results.Problem(statusCode: StatusCodes.Status404NotFound)).AllowAnonymous();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.Run();

public partial class Program;
