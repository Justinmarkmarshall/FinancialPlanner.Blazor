using Marshall.Authentication.Google;
using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinancialPlanner.Blazor.Services;

public sealed class FinancialPlannerGoogleSessionHandler(FinanceDbContext dbContext, ISessionService sessionService)
    : IGoogleSessionHandler
{
    public async Task<AuthenticationTicket?> CreateTicketAsync(HttpContext context, ClaimsPrincipal externalPrincipal, string applicationScheme)
    {
        // Extract Google claims
        var googleSubject = externalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = externalPrincipal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var displayName = externalPrincipal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        if (string.IsNullOrEmpty(googleSubject))
            return null;

        // Find or create user
        var user = await dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject);

        if (user == null)
        {
            user = new User
            {
                GoogleSubject = googleSubject,
                Email = email ?? string.Empty,
                DisplayName = displayName ?? string.Empty,
                CreatedUtc = DateTime.UtcNow
            };
            dbContext.Users.Add(user);

            // new up a user profile here as well
            user.Profile = new UserProfile
            {
                User = user,
                Currency = "GBP",
                PaydayDayOfMonth = 1,
                Locale = "en-GB"
            };
            await dbContext.SaveChangesAsync();
        }
        else
        {
            var updated = false;
            if (!string.IsNullOrEmpty(email) && user.Email != email) { user.Email = email; updated = true; }
            if (!string.IsNullOrEmpty(displayName) && user.DisplayName != displayName) { user.DisplayName = displayName; updated = true; }
            if (updated) await dbContext.SaveChangesAsync();
        }

        // Create DB-backed session (Pattern B)
        var session = await sessionService.CreateSessionAsync(
            user.Id,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString());

        // Issue YOUR cookie with app_* claims
        var claims = new List<Claim>
        {
            new("app_user_id", user.Id.ToString()),
            new("app_session_id", session.SessionId.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new("google_sub", user.GoogleSubject) // optional, informational
        };

        var identity = new ClaimsIdentity(claims, applicationScheme);
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationTicket(principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = session.ExpiresUtc
        }, applicationScheme);
    }

    public async Task<bool> ValidateAsync(HttpContext context, ClaimsPrincipal principal)
    {
        if (!Guid.TryParse(principal.FindFirstValue("app_session_id"), out var sessionId) ||
            !int.TryParse(principal.FindFirstValue("app_user_id"), out var userId))
            return false;
        var session = await sessionService.ValidateSessionAsync(sessionId);
        return session is not null && session.UserId == userId;
    }

    public async Task RevokeAsync(HttpContext context, ClaimsPrincipal principal)
    {
        if (Guid.TryParse(principal.FindFirstValue("app_session_id"), out var sessionId))
            await sessionService.RevokeSessionAsync(sessionId);
    }
}
