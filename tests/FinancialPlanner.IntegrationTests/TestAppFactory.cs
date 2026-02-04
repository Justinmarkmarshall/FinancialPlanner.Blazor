using FinancialPlanner.Blazor.DataAccess;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialPlanner.IntegrationTests
{
    /// <summary>
    /// Custom WebApplicationFactory for integration testing.
    /// Configures the app with a test SQLite database and Testing environment
    /// to enable test-only endpoints.
    /// </summary>
    public class TestAppFactory : WebApplicationFactory<Blazor.Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Set environment to "Testing" to enable test endpoints
            builder.UseEnvironment("Testing");

            // ✅ Provide dummy Google OAuth config EARLY via env vars
            Environment.SetEnvironmentVariable("Authentication__Google__ClientId", "test-client-id");
            Environment.SetEnvironmentVariable("Authentication__Google__ClientSecret", "test-client-secret");


            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<FinanceDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Remove existing DbContext registration (if registered as type)
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(FinanceDbContext));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // Use SQLite in-memory with shared connection
                // This supports ExecuteUpdateAsync unlike EF Core InMemory provider
                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();

                services.AddDbContext<FinanceDbContext>(options =>
                    options.UseSqlite(_connection));

                // Build provider and create database schema
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection?.Close();
            _connection?.Dispose();

            // Optional: clean up env vars so they don't leak to other tests
            Environment.SetEnvironmentVariable("Authentication__Google__ClientId", null);
            Environment.SetEnvironmentVariable("Authentication__Google__ClientSecret", null);
        }

        /// <summary>
        /// Resets the database by deleting all rows from all tables.
        /// Call at the start of each test to ensure a clean state.
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            // Delete in order respecting foreign key constraints
            db.UserSessions.RemoveRange(db.UserSessions);
            db.BankStatements.RemoveRange(db.BankStatements);
            db.Expenditures.RemoveRange(db.Expenditures);
            db.Incomes.RemoveRange(db.Incomes);
            db.Months.RemoveRange(db.Months);
            db.ExpenseCategories.RemoveRange(db.ExpenseCategories);
            db.IncomeCategories.RemoveRange(db.IncomeCategories);
            db.UserProfiles.RemoveRange(db.UserProfiles);
            db.Users.RemoveRange(db.Users);

            await db.SaveChangesAsync();
        }

        [CollectionDefinition("Integration", DisableParallelization = true)]
        public class IntegrationCollection { }
    }
}