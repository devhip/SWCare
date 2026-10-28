using SWCare.Api.Services;

namespace SWCare.Api.Controllers;

/// <summary>The caller needs at least one of these roles. Checked by CurrentUserFilter against the database.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireRolesAttribute(params AppRole[] roles) : Attribute
{
    public AppRole[] Roles { get; } = roles;
}

/// <summary>Usable before the user has replaced a first or reset password (everything else answers 403 PASSWORD_CHANGE_REQUIRED).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowWhilePasswordChangeRequiredAttribute : Attribute;
