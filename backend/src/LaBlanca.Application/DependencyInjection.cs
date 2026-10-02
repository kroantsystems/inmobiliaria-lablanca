using System.Globalization;
using FluentValidation;
using FluentValidation.Resources;
using LaBlanca.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<Features.Auth.AuthSessionIssuer>();
        ValidatorOptions.Global.LanguageManager = new AppLanguageManager();

        return services;
    }

    /// <summary>FluentValidation não tem guarani; usa espanhol, como o restante do sistema.</summary>
    private sealed class AppLanguageManager : LanguageManager
    {
        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es");

        public override string GetString(string key, CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentUICulture;
            return base.GetString(key, culture.TwoLetterISOLanguageName == "gn" ? Spanish : culture);
        }
    }
}
