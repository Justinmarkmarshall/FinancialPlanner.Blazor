using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using FinancialPlanner.Blazor.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlanner.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="SessionService"/>.
    /// Tests cover session creation, validation, revocation, touch functionality, and cleanup.
    /// Uses SQLite in-memory database because ExecuteUpdateAsync is not supported by EF Core InMemory provider.
    /// </summary>
    public class SessionServiceTests : IDisposable
    {
        private readonly FinanceDbContext _dbContext;
        private readonly SqliteConnection _connection;

        public SessionServiceTests()
        {
            // Use SQLite in-memory database (supports ExecuteUpdateAsync)
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<FinanceDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new FinanceDbContext(options);
            _dbContext.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }

        private SessionService CreateService()
        {
            return new SessionService(_dbContext);
        }

        /// <summary>
        /// Creates a test user in the database and returns the user entity.
        /// </summary>
        private async Task<User> CreateTestUserAsync(string googleSubject = "google-123")
        {
            var user = new User
            {
                GoogleSubject = googleSubject,
                Email = "test@example.com",
                DisplayName = "Test User",
                CreatedUtc = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// Creates a session directly in the database for testing validation/revocation.
        /// </summary>
        private async Task<UserSession> CreateTestSessionAsync(
            int userId,
            DateTime? expiresUtc = null,
            DateTime? revokedUtc = null,
            DateTime? createdUtc = null)
        {
            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                CreatedUtc = createdUtc ?? DateTime.UtcNow,
                ExpiresUtc = expiresUtc ?? DateTime.UtcNow.AddDays(7),
                RevokedUtc = revokedUtc,
                IpAddress = "127.0.0.1",
                UserAgent = "TestAgent"
            };

            _dbContext.UserSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return session;
        }

        #region CreateSessionAsync Tests

        /// <summary>
        /// Verifies that CreateSessionAsync creates a new session with a unique SessionId.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_CreatesSessionWithUniqueId()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", "Chrome/120");

            // Assert
            Assert.NotEqual(Guid.Empty, session.SessionId);
        }

        /// <summary>
        /// Verifies that CreateSessionAsync correctly stores the user ID in the session.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_StoresCorrectUserId()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", "Chrome/120");

            // Assert
            Assert.Equal(user.Id, session.UserId);
        }

        /// <summary>
        /// Verifies that CreateSessionAsync stores the IP address provided.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_StoresIpAddress()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();
            const string expectedIp = "192.168.1.100";

            // Act
            var session = await service.CreateSessionAsync(user.Id, expectedIp, "Chrome/120");

            // Assert
            Assert.Equal(expectedIp, session.IpAddress);
        }

        /// <summary>
        /// Verifies that CreateSessionAsync stores the user agent string provided.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_StoresUserAgent()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();
            const string expectedAgent = "Mozilla/5.0 Chrome/120";

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", expectedAgent);

            // Assert
            Assert.Equal(expectedAgent, session.UserAgent);
        }

        /// <summary>
        /// Verifies that CreateSessionAsync uses the default expiry of 7 days when not specified.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_UsesDefaultExpiryOf7Days()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();
            var beforeCreate = DateTime.UtcNow;

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", "Chrome/120");

            // Assert - Expiry should be approximately 7 days from now
            var expectedExpiry = beforeCreate.AddDays(7);
            Assert.True(session.ExpiresUtc >= expectedExpiry.AddSeconds(-5));
            Assert.True(session.ExpiresUtc <= expectedExpiry.AddSeconds(5));
        }

        /// <summary>
        /// Verifies that CreateSessionAsync uses a custom expiry when provided.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_UsesCustomExpiry()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();
            var customExpiry = TimeSpan.FromHours(2);
            var beforeCreate = DateTime.UtcNow;

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", "Chrome/120", customExpiry);

            // Assert
            var expectedExpiry = beforeCreate.Add(customExpiry);
            Assert.True(session.ExpiresUtc >= expectedExpiry.AddSeconds(-5));
            Assert.True(session.ExpiresUtc <= expectedExpiry.AddSeconds(5));
        }

        /// <summary>
        /// Verifies that CreateSessionAsync persists the session to the database.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_PersistsSessionToDatabase()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", "Chrome/120");

            // Assert - Verify session exists in database
            var dbSession = await _dbContext.UserSessions.FindAsync(session.SessionId);
            Assert.NotNull(dbSession);
            Assert.Equal(user.Id, dbSession.UserId);
        }

        /// <summary>
        /// Verifies that CreateSessionAsync handles null IP address gracefully.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_HandlesNullIpAddress()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();

            // Act
            var session = await service.CreateSessionAsync(user.Id, null, "Chrome/120");

            // Assert
            Assert.Null(session.IpAddress);
        }

        /// <summary>
        /// Verifies that CreateSessionAsync handles null user agent gracefully.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_HandlesNullUserAgent()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", null);

            // Assert
            Assert.Null(session.UserAgent);
        }

        /// <summary>
        /// Verifies that CreateSessionAsync sets CreatedUtc to current UTC time.
        /// </summary>
        [Fact]
        public async Task CreateSessionAsync_SetsCreatedUtcToNow()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();
            var beforeCreate = DateTime.UtcNow;

            // Act
            var session = await service.CreateSessionAsync(user.Id, "192.168.1.1", "Chrome/120");

            // Assert
            Assert.True(session.CreatedUtc >= beforeCreate.AddSeconds(-1));
            Assert.True(session.CreatedUtc <= DateTime.UtcNow.AddSeconds(1));
        }

        #endregion

        #region ValidateSessionAsync Tests

        /// <summary>
        /// Verifies that ValidateSessionAsync returns the session when it exists and is valid.
        /// </summary>
        [Fact]
        public async Task ValidateSessionAsync_ReturnsSession_WhenValid()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var service = CreateService();

            // Act
            var result = await service.ValidateSessionAsync(session.SessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(session.SessionId, result.SessionId);
        }

        /// <summary>
        /// Verifies that ValidateSessionAsync returns null when the session does not exist.
        /// </summary>
        [Fact]
        public async Task ValidateSessionAsync_ReturnsNull_WhenSessionDoesNotExist()
        {
            // Arrange
            var service = CreateService();
            var nonExistentSessionId = Guid.NewGuid();

            // Act
            var result = await service.ValidateSessionAsync(nonExistentSessionId);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies that ValidateSessionAsync returns null when the session has expired.
        /// </summary>
        [Fact]
        public async Task ValidateSessionAsync_ReturnsNull_WhenSessionExpired()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(-1));
            var service = CreateService();

            // Act
            var result = await service.ValidateSessionAsync(session.SessionId);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies that ValidateSessionAsync returns null when the session has been revoked.
        /// </summary>
        [Fact]
        public async Task ValidateSessionAsync_ReturnsNull_WhenSessionRevoked()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(
                user.Id,
                expiresUtc: DateTime.UtcNow.AddHours(1),
                revokedUtc: DateTime.UtcNow.AddMinutes(-5));
            var service = CreateService();

            // Act
            var result = await service.ValidateSessionAsync(session.SessionId);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Verifies that ValidateSessionAsync returns the session when it expires exactly at the boundary.
        /// Edge case: session is valid if ExpiresUtc > now (not >=).
        /// </summary>
        [Fact]
        public async Task ValidateSessionAsync_ReturnsSession_WhenNotYetExpired()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddSeconds(30));
            var service = CreateService();

            // Act
            var result = await service.ValidateSessionAsync(session.SessionId);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region RevokeSessionAsync Tests

        /// <summary>
        /// Verifies that RevokeSessionAsync returns true when the session exists and is revoked.
        /// </summary>
        [Fact]
        public async Task RevokeSessionAsync_ReturnsTrue_WhenSessionExists()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id);
            var service = CreateService();

            // Act
            var result = await service.RevokeSessionAsync(session.SessionId);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that RevokeSessionAsync sets the RevokedUtc timestamp.
        /// </summary>
        [Fact]
        public async Task RevokeSessionAsync_SetsRevokedUtc()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id);
            var service = CreateService();
            var beforeRevoke = DateTime.UtcNow;

            // Act
            await service.RevokeSessionAsync(session.SessionId);

            // Assert
            var dbSession = await _dbContext.UserSessions.FindAsync(session.SessionId);
            Assert.NotNull(dbSession?.RevokedUtc);
            Assert.True(dbSession.RevokedUtc >= beforeRevoke.AddSeconds(-1));
        }

        /// <summary>
        /// Verifies that RevokeSessionAsync returns false when the session does not exist.
        /// </summary>
        [Fact]
        public async Task RevokeSessionAsync_ReturnsFalse_WhenSessionDoesNotExist()
        {
            // Arrange
            var service = CreateService();
            var nonExistentSessionId = Guid.NewGuid();

            // Act
            var result = await service.RevokeSessionAsync(nonExistentSessionId);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that a revoked session is no longer valid via ValidateSessionAsync.
        /// </summary>
        [Fact]
        public async Task RevokeSessionAsync_MakesSessionInvalid()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var service = CreateService();

            // Verify session is initially valid
            var validBefore = await service.ValidateSessionAsync(session.SessionId);
            Assert.NotNull(validBefore);

            // Act
            await service.RevokeSessionAsync(session.SessionId);

            // Assert - Session should now be invalid
            var validAfter = await service.ValidateSessionAsync(session.SessionId);
            Assert.Null(validAfter);
        }

        #endregion

        #region RevokeAllSessionsAsync Tests

        /// <summary>
        /// Verifies that RevokeAllSessionsAsync revokes all active sessions for a user.
        /// </summary>
        [Fact]
        public async Task RevokeAllSessionsAsync_RevokesAllUserSessions()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session1 = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var session2 = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(2));
            var session3 = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(3));
            var service = CreateService();

            // Act
            await service.RevokeAllSessionsAsync(user.Id);

            // Assert - All sessions should be revoked (need to reload from DB)
            _dbContext.ChangeTracker.Clear();
            var dbSession1 = await _dbContext.UserSessions.FindAsync(session1.SessionId);
            var dbSession2 = await _dbContext.UserSessions.FindAsync(session2.SessionId);
            var dbSession3 = await _dbContext.UserSessions.FindAsync(session3.SessionId);

            Assert.NotNull(dbSession1?.RevokedUtc);
            Assert.NotNull(dbSession2?.RevokedUtc);
            Assert.NotNull(dbSession3?.RevokedUtc);
        }

        /// <summary>
        /// Verifies that RevokeAllSessionsAsync does not affect sessions from other users.
        /// </summary>
        [Fact]
        public async Task RevokeAllSessionsAsync_DoesNotAffectOtherUsers()
        {
            // Arrange
            var user1 = await CreateTestUserAsync("google-111");
            var user2 = await CreateTestUserAsync("google-222");
            var session1 = await CreateTestSessionAsync(user1.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var session2 = await CreateTestSessionAsync(user2.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var service = CreateService();

            // Act - Revoke only user1's sessions
            await service.RevokeAllSessionsAsync(user1.Id);

            // Assert (need to reload from DB)
            _dbContext.ChangeTracker.Clear();
            var dbSession1 = await _dbContext.UserSessions.FindAsync(session1.SessionId);
            var dbSession2 = await _dbContext.UserSessions.FindAsync(session2.SessionId);

            Assert.NotNull(dbSession1?.RevokedUtc); // User1's session is revoked
            Assert.Null(dbSession2?.RevokedUtc);    // User2's session is NOT revoked
        }

        /// <summary>
        /// Verifies that RevokeAllSessionsAsync completes without error when user has no sessions.
        /// </summary>
        [Fact]
        public async Task RevokeAllSessionsAsync_HandlesUserWithNoSessions()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();

            // Act & Assert - Should not throw
            var exception = await Record.ExceptionAsync(() => service.RevokeAllSessionsAsync(user.Id));
            Assert.Null(exception);
        }

        #endregion

        #region TouchSessionAsync Tests

        /// <summary>
        /// Verifies that TouchSessionAsync updates the LastSeenUtc timestamp.
        /// </summary>
        [Fact]
        public async Task TouchSessionAsync_UpdatesLastSeenUtc()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var service = CreateService();
            var beforeTouch = DateTime.UtcNow;

            // Act
            await service.TouchSessionAsync(session.SessionId, user.Id);

            // Assert (need to reload from DB)
            _dbContext.ChangeTracker.Clear();
            var dbSession = await _dbContext.UserSessions.FindAsync(session.SessionId);
            Assert.NotNull(dbSession?.LastSeenUtc);
            Assert.True(dbSession.LastSeenUtc >= beforeTouch.AddSeconds(-1));
        }

        /// <summary>
        /// Verifies that TouchSessionAsync returns true for a valid session.
        /// </summary>
        [Fact]
        public async Task TouchSessionAsync_ReturnsTrue_WhenSessionValid()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var service = CreateService();

            // Act
            var result = await service.TouchSessionAsync(session.SessionId, user.Id);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that TouchSessionAsync returns false when session does not exist.
        /// </summary>
        [Fact]
        public async Task TouchSessionAsync_ReturnsFalse_WhenSessionDoesNotExist()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var service = CreateService();

            // Act
            var result = await service.TouchSessionAsync(Guid.NewGuid(), user.Id);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that TouchSessionAsync returns false when session is expired.
        /// </summary>
        [Fact]
        public async Task TouchSessionAsync_ReturnsFalse_WhenSessionExpired()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(-1));
            var service = CreateService();

            // Act
            var result = await service.TouchSessionAsync(session.SessionId, user.Id);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that TouchSessionAsync returns false when session is revoked.
        /// </summary>
        [Fact]
        public async Task TouchSessionAsync_ReturnsFalse_WhenSessionRevoked()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var session = await CreateTestSessionAsync(
                user.Id,
                expiresUtc: DateTime.UtcNow.AddHours(1),
                revokedUtc: DateTime.UtcNow.AddMinutes(-5));
            var service = CreateService();

            // Act
            var result = await service.TouchSessionAsync(session.SessionId, user.Id);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Verifies that TouchSessionAsync returns false when user ID doesn't match.
        /// This prevents users from touching other users' sessions.
        /// </summary>
        [Fact]
        public async Task TouchSessionAsync_ReturnsFalse_WhenUserIdMismatch()
        {
            // Arrange
            var user1 = await CreateTestUserAsync("google-111");
            var user2 = await CreateTestUserAsync("google-222");
            var session = await CreateTestSessionAsync(user1.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var service = CreateService();

            // Act - Try to touch user1's session with user2's ID
            var result = await service.TouchSessionAsync(session.SessionId, user2.Id);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CleanupExpiredSessionsAsync Tests

        /// <summary>
        /// Verifies that CleanupExpiredSessionsAsync removes expired sessions older than 30 days.
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_RemovesOldExpiredSessions()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var oldExpiredSession = await CreateTestSessionAsync(
                user.Id,
                expiresUtc: DateTime.UtcNow.AddDays(-40),
                createdUtc: DateTime.UtcNow.AddDays(-45));
            var service = CreateService();

            // Act
            await service.CleanupExpiredSessionsAsync();

            // Assert
            var dbSession = await _dbContext.UserSessions.FindAsync(oldExpiredSession.SessionId);
            Assert.Null(dbSession);
        }

        /// <summary>
        /// Verifies that CleanupExpiredSessionsAsync removes revoked sessions older than 30 days.
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_RemovesOldRevokedSessions()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var oldRevokedSession = await CreateTestSessionAsync(
                user.Id,
                expiresUtc: DateTime.UtcNow.AddDays(7),
                revokedUtc: DateTime.UtcNow.AddDays(-35),
                createdUtc: DateTime.UtcNow.AddDays(-40));
            var service = CreateService();

            // Act
            await service.CleanupExpiredSessionsAsync();

            // Assert
            var dbSession = await _dbContext.UserSessions.FindAsync(oldRevokedSession.SessionId);
            Assert.Null(dbSession);
        }

        /// <summary>
        /// Verifies that CleanupExpiredSessionsAsync keeps expired sessions within the 30-day audit window.
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_KeepsRecentExpiredSessions()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var recentExpiredSession = await CreateTestSessionAsync(
                user.Id,
                expiresUtc: DateTime.UtcNow.AddHours(-1),
                createdUtc: DateTime.UtcNow.AddDays(-5)); // Created 5 days ago, within 30-day window
            var service = CreateService();

            // Act
            await service.CleanupExpiredSessionsAsync();

            // Assert - Session should NOT be deleted (within audit window)
            var dbSession = await _dbContext.UserSessions.FindAsync(recentExpiredSession.SessionId);
            Assert.NotNull(dbSession);
        }

        /// <summary>
        /// Verifies that CleanupExpiredSessionsAsync does not affect active sessions.
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_KeepsActiveSessions()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            var activeSession = await CreateTestSessionAsync(user.Id, expiresUtc: DateTime.UtcNow.AddHours(1));
            var service = CreateService();

            // Act
            await service.CleanupExpiredSessionsAsync();

            // Assert
            var dbSession = await _dbContext.UserSessions.FindAsync(activeSession.SessionId);
            Assert.NotNull(dbSession);
        }

        /// <summary>
        /// Verifies that CleanupExpiredSessionsAsync handles empty database gracefully.
        /// </summary>
        [Fact]
        public async Task CleanupExpiredSessionsAsync_HandlesEmptyDatabase()
        {
            // Arrange
            var service = CreateService();

            // Act & Assert - Should not throw
            var exception = await Record.ExceptionAsync(() => service.CleanupExpiredSessionsAsync());
            Assert.Null(exception);
        }

        #endregion
    }
}