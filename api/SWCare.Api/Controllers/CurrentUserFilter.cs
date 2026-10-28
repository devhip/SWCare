using System.Globalization;
using SWCare.Api.BLC;
using SWCare.Api.Data;
using SWCare.Api.Data.Interceptors;
using SWCare.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace SWCare.Api.Controllers;

/// <summary>
/// Loads the logged-in user from the database on every request, so a disabled account or a new department head
/// takes effect at once (Q41) instead of when the access token expires. Then checks the forced password change
/// and [RequireRoles].
/// </summary>
public sealed class CurrentUserFilter(SWCareDbContext db, CurrentUser currentUser, AuditContext audit) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            await next();
            return;
        }

        var subject = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(subject, CultureInfo.InvariantCulture, out var userId))
        {
            context.Result = ApiResponses.Error(StatusCodes.Status401Unauthorized, "Not logged in.", ErrorCodes.Unauthorized);
            return;
        }

        var row = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.IsActive,
                User = new UserContext(
                    u.Id,
                    u.Username,
                    u.DisplayName,
                    u.DepartmentId,
                    u.Department.Name,
                    u.Department.Kind,
                    u.IsAdmin,
                    u.Department.HeadUserId == u.Id,
                    u.MustChangePassword,
                    u.Language)
            })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (row is not { IsActive: true })
        {
            context.Result = ApiResponses.Error(StatusCodes.Status401Unauthorized, "This account is disabled.", ErrorCodes.AccountDisabled);
            return;
        }

        currentUser.Set(row.User);
        audit.Actor = row.User.Username;

        if (row.User.MustChangePassword && !metadata.OfType<AllowWhilePasswordChangeRequiredAttribute>().Any())
        {
            context.Result = ApiResponses.Error(StatusCodes.Status403Forbidden, "Change your password first.", ErrorCodes.PasswordChangeRequired);
            return;
        }

        foreach (var requirement in metadata.OfType<RequireRolesAttribute>())
        {
            if (!requirement.Roles.Any(row.User.Has))
            {
                context.Result = ApiResponses.Error(StatusCodes.Status403Forbidden, "You do not have permission for this.", ErrorCodes.Forbidden);
                return;
            }
        }

        await next();
    }
}
