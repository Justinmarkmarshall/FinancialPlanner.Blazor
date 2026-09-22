using System.Net;
using System.Security.Claims;
using Marshall.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialPlanner.IntegrationTests;

public class GoogleSessionLibraryTests
{
    [Theory]
    [InlineData("/dashboard?tab=1", "/dashboard?tab=1")]
    [InlineData("https://example.com", "/")]
    [InlineData("//example.com", "/")]
    [InlineData("/\\example.com", "/")]
    [InlineData("/\r\nLocation: https://example.com", "/")]
    public async Task CustomSchemes_CompleteLogin_AndRejectUnsafeRedirects(string returnUrl, string expected)
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        var external = await client.PostAsync("/external", null);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/complete?returnUrl=" + Uri.EscapeDataString(returnUrl));
        request.Headers.Add("Cookie", external.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location!.OriginalString);
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookies, c => c.StartsWith("Test.External=;") && c.Contains("expires="));
        var appCookie = Assert.Single(cookies.Where(c => c.StartsWith("Test.App=") && !c.StartsWith("Test.App=;")));
        using var ping = new HttpRequestMessage(HttpMethod.Get, "/protected");
        ping.Headers.Add("Cookie", appCookie.Split(';')[0]);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(ping)).StatusCode);
    }

    [Fact]
    public async Task ExternalIdentityAlone_DoesNotAuthorizeApplicationRequests()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        var external = await client.PostAsync("/external", null);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add("Cookie", external.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/sign-in", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task GoogleChallenge_UsesBuiltInHandlerAndCustomCompletionRoute()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/google");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("accounts.google.com", response.Headers.Location!.Host);
        Assert.Contains("signin-google", response.Headers.Location.Query);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains(".AspNetCore.Correlation."));
    }

    private static async Task<WebApplication> CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddGoogleSessionAuthentication<TestSessionHandler>(google =>
        {
            google.ClientId = "test-client";
            google.ClientSecret = "test-secret";
        }, options =>
        {
            options.ApplicationScheme = "app";
            options.ExternalScheme = "temporary";
            options.GoogleScheme = "provider";
            options.ApplicationCookieName = "Test.App";
            options.ExternalCookieName = "Test.External";
            options.LoginPath = "/sign-in";
            options.ChallengePath = "/google";
            options.CompletionPath = "/complete";
            options.LogoutPath = "/sign-out";
        });
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGoogleSessionEndpoints();
        app.MapGet("/protected", () => "ok").RequireAuthorization();
        app.MapPost("/external", async (HttpContext context) =>
            await context.SignInAsync("temporary", new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "subject") }, "temporary"))));
        await app.StartAsync();
        return app;
    }

    public sealed class TestSessionHandler : IGoogleSessionHandler
    {
        public Task<AuthenticationTicket?> CreateTicketAsync(HttpContext context, ClaimsPrincipal externalPrincipal, string applicationScheme) =>
            Task.FromResult<AuthenticationTicket?>(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("session", "valid") }, applicationScheme)), applicationScheme));
        public Task<bool> ValidateAsync(HttpContext context, ClaimsPrincipal principal) => Task.FromResult(principal.HasClaim("session", "valid"));
        public Task RevokeAsync(HttpContext context, ClaimsPrincipal principal) => Task.CompletedTask;
    }
}
