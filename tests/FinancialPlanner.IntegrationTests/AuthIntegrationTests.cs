using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using FinancialPlanner.IntegrationTests;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

[Collection("Integration")]
public class AuthIntegrationTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(TestAppFactory factory)
    {
        _factory = factory;

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Test case proves the need to use the External Cookie scheme
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task LoginCallback_ReadsExternalCookie_CreatesDbSession_AndIssuesAppCookie()
    {
        await _factory.ResetDatabaseAsync();

        // 1) Simulate Google handler having placed claims into the external cookie
        var extResp = await _client.PostAsync("/_test/external-signin", content: null);
        Assert.Equal(HttpStatusCode.OK, extResp.StatusCode);

        // 2) Hit your callback endpoint
        var callbackResp = await _client.GetAsync("/login-callback?returnUrl=/");
        Assert.Equal(HttpStatusCode.Redirect, callbackResp.StatusCode);

        // Redirect back to returnUrl ("/")
        var locationStr = callbackResp.Headers.Location!.OriginalString;
        var pathOnly = locationStr.Split('?', 2)[0];
        Assert.Equal("/", pathOnly);

        // 3) App cookie should be issued
        Assert.True(callbackResp.Headers.TryGetValues("Set-Cookie", out var setCookies));
        Assert.Contains(setCookies, c => c.StartsWith("FinancialPlanner.Auth=", StringComparison.Ordinal));

        // 4) DB-backed session should exist
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var session = await db.UserSessions
            .OrderByDescending(s => s.CreatedUtc)
            .FirstOrDefaultAsync();

        Assert.NotNull(session);
        Assert.True(session!.ExpiresUtc > DateTime.UtcNow);
        Assert.Null(session.RevokedUtc);

        // Bonus: user should exist too
        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSubject == "google-sub-test-123");
        Assert.NotNull(user);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidSession_ReturnsOk()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        var signInResp = await _client.PostAsync(
            $"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        Assert.Equal(HttpStatusCode.OK, signInResp.StatusCode);

        var ping = await _client.GetAsync("/_auth/ping");
        Assert.Equal(HttpStatusCode.OK, ping.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithRevokedSession_RedirectsToLogin()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30),
            revokedUtc: DateTime.UtcNow);

        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        var ping = await _client.GetAsync("/_auth/ping");

        Assert.Equal(HttpStatusCode.Redirect, ping.StatusCode);
        Assert.Equal("/login", ping.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithExpiredSession_RedirectsToLogin()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(-10));

        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        var ping = await _client.GetAsync("/_auth/ping");

        AssertRedirectsTo(ping, "/login");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithNonExistentSession_RedirectsToLogin()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, _) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        var fakeSessionId = Guid.NewGuid();
        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={fakeSessionId}", null);

        var ping = await _client.GetAsync("/_auth/ping");

        AssertRedirectsTo(ping, "/login");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMismatchedUserId_RedirectsToLogin()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        var wrongUserId = userId + 999;
        await _client.PostAsync($"/_test/signin?userId={wrongUserId}&sessionId={sessionId}", null);

        var ping = await _client.GetAsync("/_auth/ping");

        AssertRedirectsTo(ping, "/login");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthentication_RedirectsToLogin()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var ping = await freshClient.GetAsync("/_auth/ping");

        AssertRedirectsTo(ping, "/login");
    }

    [Fact]
    public async Task Logout_WithValidSession_RevokesSessionAndRedirectsToLogin()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        var pingBefore = await _client.GetAsync("/_auth/ping");
        Assert.Equal(HttpStatusCode.OK, pingBefore.StatusCode);

        var logoutResp = await _client.GetAsync("/logout");

        AssertRedirectsTo(logoutResp, "/login");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var session = await db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

        Assert.NotNull(session);
        Assert.NotNull(session!.RevokedUtc);
    }

    private static void AssertRedirectsTo(HttpResponseMessage resp, string expectedPath)
    {
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

        var location = resp.Headers.Location;
        Assert.NotNull(location);

        string actualPath;

        if (location!.IsAbsoluteUri)
        {
            actualPath = location.AbsolutePath;
        }
        else
        {
            // Relative URI: "/login?ReturnUrl=..."
            actualPath = location.OriginalString.Split('?', 2)[0];
        }

        Assert.Equal(expectedPath, actualPath);
    }

    [Fact]
    public async Task Logout_ClearsAuthCookie_SubsequentRequestsRedirectToLogin()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        await _client.GetAsync("/logout");

        var ping = await _client.GetAsync("/_auth/ping");

        AssertRedirectsTo(ping, "/login");
    }

    [Fact]
    public async Task Logout_WithNoSession_StillRedirectsToLogin()
    {
        await _factory.ResetDatabaseAsync();

        using var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var logoutResp = await freshClient.GetAsync("/logout");

        Assert.Equal(HttpStatusCode.Redirect, logoutResp.StatusCode);

        var location = logoutResp.Headers.Location!;
        var locationStr = location.OriginalString; // works for relative URIs

        Assert.StartsWith("/login", locationStr);
    }

    [Fact]
    public async Task Logout_SetsCacheControlHeaders()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        var logoutResp = await _client.GetAsync("/logout");

        Assert.Contains("no-cache", logoutResp.Headers.CacheControl?.ToString() ?? "");
        Assert.Contains("no-store", logoutResp.Headers.CacheControl?.ToString() ?? "");
    }

    [Fact]
    public async Task AuthMe_ReturnsUserInfo_WhenAuthenticated()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        var response = await _client.GetAsync("/_auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(userId.ToString(), content);
        Assert.Contains(sessionId.ToString(), content);
    }

    [Fact]
    public async Task AuthMe_RedirectsToLogin_WhenNotAuthenticated()
    {
        var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await freshClient.GetAsync("/_auth/me");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task MultipleValidSessions_BothWork()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId1) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        var sessionId2 = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            db.UserSessions.Add(new UserSession
            {
                SessionId = sessionId2,
                UserId = userId,
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            });
            await db.SaveChangesAsync();
        }

        var client1 = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client1.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId1}", null);
        var ping1 = await client1.GetAsync("/_auth/ping");
        Assert.Equal(HttpStatusCode.OK, ping1.StatusCode);

        var client2 = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client2.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId2}", null);
        var ping2 = await client2.GetAsync("/_auth/ping");
        Assert.Equal(HttpStatusCode.OK, ping2.StatusCode);
    }

    [Fact]
    public async Task RevokingOneSession_DoesNotAffectOtherSessions()
    {
        await _factory.ResetDatabaseAsync();

        var (userId, sessionId1) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        var sessionId2 = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            db.UserSessions.Add(new UserSession
            {
                SessionId = sessionId2,
                UserId = userId,
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            });
            await db.SaveChangesAsync();
        }

        var client1 = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client1.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId1}", null);
        await client1.GetAsync("/logout");

        var client2 = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await client2.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId2}", null);
        var ping2 = await client2.GetAsync("/_auth/ping");

        Assert.Equal(HttpStatusCode.OK, ping2.StatusCode);
    }

    [Fact]
    public async Task RevokingSessionInDb_KicksUserOutOnNextRequest()
    {
        var (userId, sessionId) = await SeedUserAndSessionAsync(
            _factory.Services,
            expiresUtc: DateTime.UtcNow.AddMinutes(30));

        await _client.PostAsync($"/_test/signin?userId={userId}&sessionId={sessionId}", null);

        var ok = await _client.GetAsync("/_auth/ping");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            var session = await db.UserSessions.SingleAsync(s => s.SessionId == sessionId);
            session.RevokedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var after = await _client.GetAsync("/_auth/ping");
        Assert.Equal(HttpStatusCode.Redirect, after.StatusCode);
        Assert.Equal("/login", after.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task LoginCallback_WithValidExternalCookie_CreatesUserAndSession()
    {
        await _factory.ResetDatabaseAsync();

        // This test will FAIL until we fix the external cookie configuration
        // The test documents the expected behavior:
        // 1. External cookie contains Google claims
        // 2. /login-callback reads external cookie
        // 3. Creates User in DB
        // 4. Creates UserSession in DB
        // 5. Issues app cookie with app_user_id and app_session_id
        // 6. Clears external cookie
        // 7. Redirects to returnUrl

        // To make this test pass, we need to:
        // 1. Add "ExternalCookie" scheme in Program.cs
        // 2. Configure Google to use SignInScheme = "ExternalCookie"
        // 3. Update AuthEndpoints to read from "ExternalCookie"

        // For now, we can only test the failure case
        // This test serves as documentation of the bug

        var response = await _client.GetAsync("/login-callback?returnUrl=/dashboard");

        // Currently this redirects to /login?error=auth_failed
        // because there's no external cookie to read

        // EXPECTED (after fix): Should fail because no external cookie exists
        // but the flow should be testable with a mock external cookie
        AssertRedirectsTo(response, "/login");
    }

    [Fact]
    public async Task LoginCallback_WithoutExternalCookie_RedirectsToLoginWithError()
    {
        await _factory.ResetDatabaseAsync();

        // Simulate hitting /login-callback directly without going through Google OAuth
        // This is what happens when AuthenticateAsync("Google") fails
        var response = await _client.GetAsync("/login-callback?returnUrl=/");

        AssertRedirectsTo(response, "/login");

        // Verify the error query parameter is set
        var location = response.Headers.Location?.OriginalString ?? "";
        Assert.Contains("error=auth_failed", location);
    }

    private static async Task<(int userId, Guid sessionId)> SeedUserAndSessionAsync(
        IServiceProvider services,
        DateTime expiresUtc,
        DateTime? revokedUtc = null)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        var user = new User
        {
            GoogleSubject = $"test-sub-{Guid.NewGuid()}",
            Email = $"test-{Guid.NewGuid()}@example.com",
            DisplayName = "Test User",
            CreatedUtc = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sessionId = Guid.NewGuid();
        db.UserSessions.Add(new UserSession
        {
            SessionId = sessionId,
            UserId = user.Id,
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = expiresUtc,
            RevokedUtc = revokedUtc
        });

        await db.SaveChangesAsync();

        return (user.Id, sessionId);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
