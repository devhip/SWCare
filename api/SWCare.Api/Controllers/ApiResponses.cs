using System.Text.Json;
using SWCare.Api.BLC;
using SWCare.Api.DTO.Res;
using Microsoft.AspNetCore.Mvc;

namespace SWCare.Api.Controllers;

/// <summary>Error envelopes for the places that are not a controller action: exception handler, JWT challenge, filters.</summary>
internal static class ApiResponses
{
    /// <summary>PascalCase, the same as the MVC output.</summary>
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = null };

    public static Task WriteAsync(HttpResponse response, int status, string message, string code, CancellationToken cancellationToken = default)
    {
        response.StatusCode = status;
        return response.WriteAsJsonAsync(ApiResult.Fail<object>(message, code), Json, cancellationToken);
    }

    public static ObjectResult Error(int status, string message, string code) =>
        new(ApiResult.Fail<object>(message, code)) { StatusCode = status };

    /// <summary>400 for model binding and FluentValidation errors (ApiBehaviorOptions.InvalidModelStateResponseFactory).</summary>
    public static IActionResult ValidationFailed(ActionContext context)
    {
        var messages = context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct();
        return Error(StatusCodes.Status400BadRequest, string.Join(" ", messages), ErrorCodes.ValidationFailed);
    }
}
