using System.Text.Json;
using FluentValidation;
using SWCare.Api.BLC;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.Controllers;

/// <summary>Turns exceptions into the error envelope.</summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message, code) = exception switch
        {
            ApiException api => (api.StatusCode, api.Message, api.Code),

            // Validation run by hand, e.g. the JSON part of a multipart request.
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage).Distinct()),
                ErrorCodes.ValidationFailed),
            JsonException => (StatusCodes.Status400BadRequest, "The JSON is not valid.", ErrorCodes.ValidationFailed),

            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was changed meanwhile, try again.", ErrorCodes.ConcurrencyConflict),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong, try again.", ErrorCodes.ServerError)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled error on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        await ApiResponses.WriteAsync(httpContext.Response, status, message, code, cancellationToken);
        return true;
    }
}
