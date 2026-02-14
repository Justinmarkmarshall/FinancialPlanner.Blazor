using FinancialPlanner.Blazor.Components;
using FinancialPlanner.Blazor.Components.Pages;
using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using FinancialPlanner.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;

namespace FinancialPlanner.Blazor
{
    public class Program
    {


        public static void Main(string[] args)
        {

            const string AppCookieScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "Cookies"
            const string ExternalScheme = "External";

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // EF Core SQLite
            builder.Services.AddDbContext<FinanceDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("FinanceDb")));

            builder.Services.AddScoped<ICashflowService, CashflowService>();
            builder.Services.AddScoped<IHistoryService, HistoryService>();
            builder.Services.AddScoped<IDbService, DbService>();
            builder.Services.AddScoped<IBankStatementImportService, BankStatementImportService>();
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Google Auth
            // Most apps do this:
            // Unauthenticated request → redirect to / login
            // User clicks “Continue with Google” on / login → then challenge Google
            // That means:
            // Default challenge scheme should be Cookie, not Google.
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = AppCookieScheme;
                    // default chalnge scheme is cookie to avoid immediate redirect to Google on unauthenticated requests and instead go to /login
                    // that way users trigger the Google challenge themselves
                    options.DefaultChallengeScheme = AppCookieScheme;
                })
                .AddCookie(AppCookieScheme, options =>
                {
                    options.Cookie.Name = "FinancialPlanner.Auth";
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;

                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                    // Validate session on every request (Pattern B)
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        var sessionIdClaim = context.Principal?.FindFirstValue("app_session_id");
                        var userIdClaim = context.Principal?.FindFirstValue("app_user_id");

                        if (!Guid.TryParse(sessionIdClaim, out var sessionId) ||
                            !int.TryParse(userIdClaim, out var userId))
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                        var session = await sessionService.ValidateSessionAsync(sessionId);

                        if (session == null || session.UserId != userId)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                    };
                })
                .AddCookie(ExternalScheme, options =>
                {
                    options.Cookie.Name = "FinancialPlanner.External";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                    options.SlidingExpiration = false;

                    options.Cookie.SameSite = SameSiteMode.None;         // IMPORTANT for OAuth round-trip
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddGoogle("Google", options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
                            ?? throw new InvalidOperationException("Missing Authentication:Google:ClientId");
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
                            ?? throw new InvalidOperationException("Missing Authentication:Google:ClientSecret");

                    // this line uses the temporary cookie scheme to store info from Google during the auth process
                    options.SignInScheme = ExternalScheme;
                });

            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();

            var dp = builder.Services.AddDataProtection();

            if (builder.Environment.IsEnvironment("Testing"))
            {
                // Avoid filesystem writes in CI integration tests
                dp.UseEphemeralDataProtectionProvider();
            }
            else
            {
                // Persist in the cluster so cookies/antiforgery survive pod restarts
                dp.PersistKeysToFileSystem(new DirectoryInfo("/app/data/dp-keys"));
            }


            // If you're behind ingress/proxy later, this avoids callback URL weirdness:
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                         | ForwardedHeaders.XForwardedProto
                                         | ForwardedHeaders.XForwardedHost;

                // In private cloud you'll often need this because the proxy IP won't match "known" networks
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            if (!app.Environment.IsEnvironment("Testing"))
            {
                // Apply pending migrations on startup - by default wil apply all the Migrations.Local - should be fine.
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                    db.Database.EnsureCreated();
                }
            }
            

            // these need to be known networks/proxies when deployed.
            app.UseForwardedHeaders();

            // test endpoint to verify forwarded headers are working in private cloud - check logs for the output
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/signin-google"))
                {
                    app.Logger.LogInformation("scheme={Scheme} host={Host} xfp={XFP} xfh={XFH}",
                        ctx.Request.Scheme,
                        ctx.Request.Host.Value,
                        ctx.Request.Headers["X-Forwarded-Proto"].ToString(),
                        ctx.Request.Headers["X-Forwarded-Host"].ToString());
                }
                await next();
            });


            // on private cloud, Cloudflare terminates TLS, then talks to origin.
            // if cloudflare connects to the origin over HTTP, UseHttpsRedirection() can create confusing loops
            // so remove this when running in the cluster
            //app.UseHttpsRedirection();

            // cookies auth and map endpoints behave best with routing enabled
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseAntiforgery();

            // Map authentication endpoints
            FinancialPlanner.Blazor.Endpoints.AuthEndpoints.MapAuthEndpoints(app);

            // Test endpoints (only available in Testing environment)
            if (app.Environment.IsEnvironment("Testing"))
            {
                MapTestEndpoints(app);
            }

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }

        /// <summary>
        /// Maps test-only endpoints for integration testing.
        /// These endpoints are only available when ASPNETCORE_ENVIRONMENT=Testing.
        /// </summary>
        private static void MapTestEndpoints(WebApplication app)
        {
            // Test sign-in endpoint - creates a cookie with app_user_id and app_session_id
            app.MapPost("/_test/signin", async (HttpContext ctx, int userId, Guid sessionId) =>
            {
                var claims = new List<Claim>
                {
                    new("app_user_id", userId.ToString()),
                    new("app_session_id", sessionId.ToString()),
                    new(ClaimTypes.Name, "Test User"),
                    new(ClaimTypes.Email, "test@example.com")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return Results.Ok("signed-in");
            }).AllowAnonymous();

            // Auth ping endpoint - protected endpoint to verify auth status
            app.MapGet("/_auth/ping", () => Results.Ok("ok"))
                .RequireAuthorization();

            // Get current user info - useful for verifying claims
            app.MapGet("/_auth/me", (HttpContext ctx) =>
            {
                var userId = ctx.User.FindFirstValue("app_user_id");
                var sessionId = ctx.User.FindFirstValue("app_session_id");
                var name = ctx.User.FindFirstValue(ClaimTypes.Name);

                return Results.Ok(new { userId, sessionId, name });
            }).RequireAuthorization();

            //writes external cookie for testing external signin
            app.MapPost("/_test/external-signin", async (HttpContext ctx) =>
            {
                const string ExternalScheme = "External";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "google-sub-test-123"),
                    new Claim(ClaimTypes.Email, "test@example.com"),
                    new Claim(ClaimTypes.Name, "Test Person"),
                };

                var identity = new ClaimsIdentity(claims, ExternalScheme);
                var principal = new ClaimsPrincipal(identity);

                await ctx.SignInAsync(ExternalScheme, principal);
                return Results.Ok("external-signed-in");
            }).AllowAnonymous();
        }
    }
}
