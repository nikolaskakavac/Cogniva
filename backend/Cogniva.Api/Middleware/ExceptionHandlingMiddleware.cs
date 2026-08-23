using Microsoft.AspNetCore.Mvc;

namespace Cogniva.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (exception is ApiException apiException)
            {
                logger.LogWarning(
                    "Request {Method} {Path} failed with status {StatusCode}: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    apiException.StatusCode,
                    apiException.Message);
            }
            else
            {
                logger.LogError(exception,
                    "An unhandled exception occurred while processing {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            var statusCode = exception is ApiException knownException
                ? knownException.StatusCode
                : StatusCodes.Status500InternalServerError;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = exception is ApiException apiError
                    ? apiError.Title
                    : "An unexpected error occurred.",
                Detail = exception is ApiException
                    ? exception.Message
                    : "The server could not complete the request.",
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json");
        }
    }
}
