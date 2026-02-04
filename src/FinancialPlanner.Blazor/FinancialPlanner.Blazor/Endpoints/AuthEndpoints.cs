using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using FinancialPlanner.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinancialPlanner.Blazor.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            // Initiates Google OAuth flow
            // OAuth providers are not my contract so not covered in Integreation tests
            app.MapGet("/login-google", async (HttpContext context, string? returnUrl) =>
            {
                var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
                if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Relative))
                    redirectUri = "/";
                var properties = new AuthenticationProperties
                {
                    RedirectUri = $"/login-callback?returnUrl={Uri.EscapeDataString(redirectUri)}"
                };
                await context.ChallengeAsync("Google", properties);
            });

            // Handles the callback after Google authentication
            app.MapGet("/login-callback", async (
                    HttpContext context,
                    FinanceDbContext dbContext,
                    ISessionService sessionService,
                    string? returnUrl) =>
            {
                var redirect = "/";
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) &&
                    returnUrl.StartsWith("/", StringComparison.Ordinal))
                {
                    redirect = returnUrl;
                }

                // Authenticate the external scheme
                var result = await context.AuthenticateAsync("External");

                if (!result.Succeeded || result.Principal == null)
                    return Results.Redirect("/login?error=auth_failed");

                // Extract Google claims
                var googleSubject = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = result.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
                var displayName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

                if (string.IsNullOrEmpty(googleSubject))
                    return Results.Redirect("/login?error=auth_failed");

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

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = session.ExpiresUtc, 
                        RedirectUri = redirect
                    });

                // clear temporary external cookie
                await context.SignOutAsync("External");
                return Results.Redirect(redirect);
            });


            // Logout endpoint - sign out from all authentication schemes
            app.MapGet("/logout", async (HttpContext context, ISessionService sessionService) =>
            {
                var sessionIdStr = context.User.FindFirstValue("app_session_id");
                if (Guid.TryParse(sessionIdStr, out var sessionId))
                    await sessionService.RevokeSessionAsync(sessionId);

                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";

                return Results.Redirect("/login");
            }).AllowAnonymous();
        }
    }
}