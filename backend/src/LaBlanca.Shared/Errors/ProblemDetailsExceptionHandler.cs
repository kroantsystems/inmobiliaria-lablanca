using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LaBlanca.Shared.Errors;

public sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetails,
    ExceptionProblemMapper mapper,
    IHostEnvironment environment,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = mapper.Map(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request rejected with {Status} ({ExceptionType})", problem.Status, exception.GetType().Name);
        }

        var details = new ProblemDetails { Status = problem.Status, Detail = problem.Detail };
        if (problem.Errors is not null)
        {
            details.Extensions["errors"] = problem.Errors;
        }

        if (environment.IsDevelopment() && problem.Status >= StatusCodes.Status500InternalServerError)
        {
            details.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = problem.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = details,
            Exception = exception,
        });
    }
}
