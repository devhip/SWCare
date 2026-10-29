using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.Data.Interceptors;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class AuthControl(
    SWCareDbContext db,
    JwtService jwt,
    CurrentUser currentUser,
    AuditContext audit,
    TimeProvider clock) : IAuthControl
{
    public async Task<LoginRes> LoginAsync(LoginReq request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await db.Users.Include(u => u.Department)
            .SingleOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Wrong username or password.", ErrorCodes.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("This account is disabled.", ErrorCodes.AccountDisabled);
        }

        audit.Actor = user.Username;
        user.LastLoginAt = clock.GetUtcNow().UtcDateTime;
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<LoginRes> RefreshAsync(RefreshTokenReq request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = JwtService.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Department)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= now)
        {
            throw new UnauthorizedException("Log in again.", ErrorCodes.InvalidRefreshToken);
        }

        if (!stored.User.IsActive)
        {
            throw new UnauthorizedException("This account is disabled.", ErrorCodes.AccountDisabled);
        }

        audit.Actor = stored.User.Username;
        stored.RevokedAt = now;
        return await IssueTokensAsync(stored.User, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenReq request, CancellationToken cancellationToken)
    {
        var hash = JwtService.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, cancellationToken);
        if (stored is null)
        {
            return;
        }

        stored.RevokedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<LoginRes> ChangePasswordAsync(ChangePasswordReq request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.Department)
            .SingleAsync(u => u.Id == currentUser.User.Id, cancellationToken);

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessRuleException("The current password is wrong.", ErrorCodes.WrongCurrentPassword);
        }

        EnsureNotUsername(request.NewPassword, user.Username);

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        await RevokeAllRefreshTokensAsync(db, user.Id, clock, cancellationToken);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<MeRes> GetMeAsync(CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Department)
            .SingleAsync(u => u.Id == currentUser.User.Id, cancellationToken);
        return ToMe(user);
    }

    public async Task<MeRes> SetLanguageAsync(SetLanguageReq request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.Department)
            .SingleAsync(u => u.Id == currentUser.User.Id, cancellationToken);
        user.Language = request.Language;
        await db.SaveChangesAsync(cancellationToken);
        return ToMe(user);
    }

    /// <summary>Also used when the Admin resets a password or disables an account.</summary>
    internal static Task RevokeAllRefreshTokensAsync(SWCareDbContext db, int userId, TimeProvider clock, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(setter => setter.SetProperty(t => t.RevokedAt, now), cancellationToken);
    }

    internal static void EnsureNotUsername(string password, string username)
    {
        if (string.Equals(password, username, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("The password cannot be the username.", ErrorCodes.PasswordSameAsUsername);
        }
    }

    private async Task<LoginRes> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = jwt.CreateAccessToken(user);
        var refresh = jwt.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = refresh.Hash, ExpiresAt = refresh.ExpiresAt });
        await db.SaveChangesAsync(cancellationToken);
        return new LoginRes(ToMe(user), accessToken, expiresAt, refresh.Token);
    }

    private static MeRes ToMe(User user)
    {
        var isHead = user.Department.HeadUserId == user.Id;
        return new MeRes(
            user.Id,
            user.Username,
            user.DisplayName,
            user.Email,
            user.DepartmentId,
            user.Department.Name,
            user.Department.Kind,
            user.IsAdmin,
            isHead,
            IsSupportHead: isHead && user.Department.Kind == DepartmentKind.Support,
            IsProgrammer: user.Department.Kind == DepartmentKind.Software,
            IsProgrammerLead: isHead && user.Department.Kind == DepartmentKind.Software,
            user.IsExecutive,
            user.CanViewAllTickets,
            user.MustChangePassword,
            user.Language);
    }
}
