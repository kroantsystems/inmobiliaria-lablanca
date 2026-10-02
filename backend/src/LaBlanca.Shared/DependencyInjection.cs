using System.Diagnostics;
using LaBlanca.Shared.Errors;
using LaBlanca.Shared.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LaBlanca.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services, Action<ExceptionMappingOptions>? configure = null)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = Customize);
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddSingleton<ExceptionProblemMapper>();
        services.Configure(configure ?? (_ => { }));
        return services;
    }

    private static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;

        var titleKey = MessageKeys.ProblemTitle(status);
        var title = AppMessages.Get(titleKey);
        if (title != titleKey)
        {
            problem.Title = title;
        }

        problem.Detail ??= status switch
        {
            StatusCodes.Status401Unauthorized => AppMessages.Get(MessageKeys.Unauthorized),
            StatusCodes.Status403Forbidden => AppMessages.Get(MessageKeys.Forbidden),
            StatusCodes.Status404NotFound => AppMessages.Get(MessageKeys.NotFound),
            StatusCodes.Status429TooManyRequests => AppMessages.Get(MessageKeys.TooManyRequests),
            _ => null,
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    }
}
