using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace FinancialPlanner.UnitTests
{
	/// <summary>
	/// Unit tests for <see cref="CurrentUserService"/>.
	/// Tests cover authentication state handling, user retrieval, caching behavior, and edge cases.
	/// </summary>
	public class CurrentUserServiceTests : IDisposable
	{
		private readonly FinanceDbContext _dbContext;
		private readonly Mock<AuthenticationStateProvider> _authStateProviderMock;

		public CurrentUserServiceTests()
		{
			// Create in-memory database for each test to ensure isolation
			var options = new DbContextOptionsBuilder<FinanceDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			_dbContext = new FinanceDbContext(options);
			_authStateProviderMock = new Mock<AuthenticationStateProvider>();
		}

		public void Dispose()
		{
			_dbContext.Dispose();
		}

		private CurrentUserService CreateService()
		{
			return new CurrentUserService(_authStateProviderMock.Object, _dbContext);
		}

		/// <summary>
		/// Sets up the mock authentication state with an authenticated user.
		/// </summary>
		/// <param name="userId">The user ID to include in the app_user_id claim.</param>
		/// <param name="sessionId">Optional session ID to include in claims.</param>
		private void SetupAuthenticatedUser(int userId, string? sessionId = null)
		{
			var claims = new List<Claim>
			{
				new("app_user_id", userId.ToString()),
				new(ClaimTypes.Name, "Test User"),
				new(ClaimTypes.Email, "test@example.com")
			};

			if (sessionId != null)
			{
				claims.Add(new Claim("session_id", sessionId));
			}

			var identity = new ClaimsIdentity(claims, "TestAuth");
			var principal = new ClaimsPrincipal(identity);
			var authState = new AuthenticationState(principal);

			_authStateProviderMock
				.Setup(x => x.GetAuthenticationStateAsync())
				.ReturnsAsync(authState);
		}

		/// <summary>
		/// Sets up the mock authentication state with an unauthenticated user (no identity).
		/// </summary>
		private void SetupUnauthenticatedUser()
		{
			var principal = new ClaimsPrincipal(new ClaimsIdentity());
			var authState = new AuthenticationState(principal);

			_authStateProviderMock
				.Setup(x => x.GetAuthenticationStateAsync())
				.ReturnsAsync(authState);
		}

		/// <summary>
		/// Creates a test user in the in-memory database with optional custom values.
		/// </summary>
		private async Task<User> CreateTestUserAsync(
			string googleSubject = "google-123",
			string email = "test@example.com",
			string displayName = "Test User")
		{
			var user = new User
			{
				GoogleSubject = googleSubject,
				Email = email,
				DisplayName = displayName,
				CreatedUtc = DateTime.UtcNow,
				Profile = new UserProfile
				{
					Currency = "GBP",
					PaydayDayOfMonth = 1,
					Locale = "en-GB"
				}
			};

			_dbContext.Users.Add(user);
			await _dbContext.SaveChangesAsync();

			return user;
		}

		#region GetCurrentUserAsync Tests

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the user is not authenticated.
		/// This ensures unauthenticated requests don't return user data.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WhenNotAuthenticated_ReturnsNull()
		{
			// Arrange
			SetupUnauthenticatedUser();
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns the correct user when authenticated
		/// and the user exists in the database.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WhenAuthenticated_UserExists_ReturnsUser()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			SetupAuthenticatedUser(user.Id);
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.NotNull(result);
			Assert.Equal(user.Id, result.Id);
			Assert.Equal(user.GoogleSubject, result.GoogleSubject);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when authenticated but the
		/// user ID in the claims doesn't exist in the database.
		/// This handles cases where a user was deleted but still has a valid token.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WhenAuthenticated_UserDoesNotExist_ReturnsNull()
		{
			// Arrange
			SetupAuthenticatedUser(userId: 9999); // Non-existent user ID
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync includes the related UserProfile entity.
		/// This ensures eager loading of the Profile navigation property works correctly.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_IncludesUserProfile()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			SetupAuthenticatedUser(user.Id);
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.NotNull(result);
			Assert.NotNull(result.Profile);
			Assert.Equal("GBP", result.Profile.Currency);
			Assert.Equal(1, result.Profile.PaydayDayOfMonth);
			Assert.Equal("en-GB", result.Profile.Locale);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync caches the user and returns the same instance
		/// on subsequent calls. This reduces unnecessary database queries.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_CachesUserOnSubsequentCalls()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			SetupAuthenticatedUser(user.Id);
			var service = CreateService();

			// Act - Call twice
			var result1 = await service.GetCurrentUserAsync();
			var result2 = await service.GetCurrentUserAsync();

			// Assert - Should be same object reference (cached)
			Assert.Same(result1, result2);
		}

		/// <summary>
		/// Verifies that the cache is invalidated when the authenticated user changes.
		/// This ensures users don't see stale data when switching accounts.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_ClearsCacheWhenUserChanges()
		{
			// Arrange
			var user1 = await CreateTestUserAsync("google-111", "user1@example.com", "User One");
			var user2 = await CreateTestUserAsync("google-222", "user2@example.com", "User Two");

			var service = CreateService();

			// Act - Get first user
			SetupAuthenticatedUser(user1.Id);
			var result1 = await service.GetCurrentUserAsync();

			// Switch to second user (simulates different user logging in)
			SetupAuthenticatedUser(user2.Id);
			var result2 = await service.GetCurrentUserAsync();

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.Equal(user1.Id, result1.Id);
			Assert.Equal(user2.Id, result2.Id);
			Assert.NotSame(result1, result2);
		}

		/// <summary>
		/// Verifies that the cache is cleared when a user logs out.
		/// This ensures no user data persists after logout.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_ClearsCacheWhenUserLogsOut()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			var service = CreateService();

			// Get user while authenticated
			SetupAuthenticatedUser(user.Id);
			var result1 = await service.GetCurrentUserAsync();
			Assert.NotNull(result1);

			// User logs out
			SetupUnauthenticatedUser();
			var result2 = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result2);
		}

		/// <summary>
		/// Verifies that multiple calls with the same user ID return the cached instance.
		/// This confirms caching behavior over multiple invocations.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_ReturnsSameUserWhenUserIdUnchanged()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			SetupAuthenticatedUser(user.Id);
			var service = CreateService();

			// Act - Call multiple times with same user ID
			var result1 = await service.GetCurrentUserAsync();
			var result2 = await service.GetCurrentUserAsync();
			var result3 = await service.GetCurrentUserAsync();

			// Assert - All should be the same cached instance
			Assert.Same(result1, result2);
			Assert.Same(result2, result3);
		}

		#endregion

		#region GetCurrentUserIdAsync Tests

		/// <summary>
		/// Verifies that GetCurrentUserIdAsync returns the correct user ID when authenticated.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserIdAsync_WhenAuthenticated_ReturnsUserId()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			SetupAuthenticatedUser(user.Id);
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserIdAsync();

			// Assert
			Assert.NotNull(result);
			Assert.Equal(user.Id, result.Value);
		}

		/// <summary>
		/// Verifies that GetCurrentUserIdAsync returns null when not authenticated.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserIdAsync_WhenNotAuthenticated_ReturnsNull()
		{
			// Arrange
			SetupUnauthenticatedUser();
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserIdAsync();

			// Assert
			Assert.Null(result);
		}

		/// <summary>
		/// Verifies that GetCurrentUserIdAsync returns null when the user ID in claims
		/// doesn't exist in the database.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserIdAsync_WhenUserDoesNotExist_ReturnsNull()
		{
			// Arrange
			SetupAuthenticatedUser(userId: 9999);
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserIdAsync();

			// Assert
			Assert.Null(result);
		}

		#endregion

		#region IsAuthenticatedAsync Tests

		/// <summary>
		/// Verifies that IsAuthenticatedAsync returns true when the user is authenticated
		/// AND exists in the database.
		/// </summary>
		[Fact]
		public async Task IsAuthenticatedAsync_WhenAuthenticatedAndUserExists_ReturnsTrue()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			SetupAuthenticatedUser(user.Id);
			var service = CreateService();

			// Act
			var result = await service.IsAuthenticatedAsync();

			// Assert
			Assert.True(result);
		}

		/// <summary>
		/// Verifies that IsAuthenticatedAsync returns false when not authenticated.
		/// </summary>
		[Fact]
		public async Task IsAuthenticatedAsync_WhenNotAuthenticated_ReturnsFalse()
		{
			// Arrange
			SetupUnauthenticatedUser();
			var service = CreateService();

			// Act
			var result = await service.IsAuthenticatedAsync();

			// Assert
			Assert.False(result);
		}

		/// <summary>
		/// Verifies that IsAuthenticatedAsync returns false when authenticated via claims
		/// but the user doesn't exist in the database.
		/// This is a security measure to prevent access with orphaned tokens.
		/// </summary>
		[Fact]
		public async Task IsAuthenticatedAsync_WhenAuthenticatedButUserNotInDb_ReturnsFalse()
		{
			// Arrange
			SetupAuthenticatedUser(userId: 9999); // User not in database
			var service = CreateService();

			// Act
			var result = await service.IsAuthenticatedAsync();

			// Assert
			Assert.False(result);
		}

		#endregion

		#region ClearCache Tests

		/// <summary>
		/// Verifies that ClearCache removes the cached user, forcing a fresh database query
		/// on the next GetCurrentUserAsync call.
		/// </summary>
		[Fact]
		public async Task ClearCache_RemovesCachedUser()
		{
			// Arrange
			var user = await CreateTestUserAsync();
			SetupAuthenticatedUser(user.Id);
			var service = CreateService();

			// Get user to populate cache
			var result1 = await service.GetCurrentUserAsync();
			Assert.NotNull(result1);

			// Act - Clear cache
			service.ClearCache();

			// Get user again (should query database again, returning new instance)
			var result2 = await service.GetCurrentUserAsync();

			// Assert - Different object references means cache was cleared
			Assert.NotNull(result2);
			Assert.NotSame(result1, result2);
			Assert.Equal(result1.Id, result2.Id);
		}

		/// <summary>
		/// Verifies that ClearCache doesn't throw an exception when called on an empty cache.
		/// This ensures safe calls without needing to check cache state first.
		/// </summary>
		[Fact]
		public void ClearCache_DoesNotThrowWhenCacheIsEmpty()
		{
			// Arrange
			var service = CreateService();

			// Act & Assert - Should not throw
			var exception = Record.Exception(() => service.ClearCache());
			Assert.Null(exception);
		}

		/// <summary>
		/// Verifies that after clearing the cache, a different user can be loaded.
		/// This simulates the logout/login flow with different accounts.
		/// </summary>
		[Fact]
		public async Task ClearCache_AllowsNewUserToBeLoaded()
		{
			// Arrange
			var user1 = await CreateTestUserAsync("google-111", "user1@example.com", "User One");
			var user2 = await CreateTestUserAsync("google-222", "user2@example.com", "User Two");

			SetupAuthenticatedUser(user1.Id);
			var service = CreateService();

			// Load first user
			var result1 = await service.GetCurrentUserAsync();
			Assert.Equal(user1.Id, result1!.Id);

			// Clear cache and switch user
			service.ClearCache();
			SetupAuthenticatedUser(user2.Id);

			// Load second user
			var result2 = await service.GetCurrentUserAsync();

			// Assert
			Assert.Equal(user2.Id, result2!.Id);
		}

		#endregion

		#region Edge Cases - Invalid Claims

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the app_user_id claim
		/// contains a non-numeric value that can't be parsed as an integer.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WithInvalidAppUserId_ReturnsNull()
		{
			// Arrange - Set up claims with non-numeric app_user_id
			var claims = new List<Claim>
			{
				new("app_user_id", "not-a-number"),
				new(ClaimTypes.Name, "Test User")
			};
			var identity = new ClaimsIdentity(claims, "TestAuth");
			var principal = new ClaimsPrincipal(identity);
			var authState = new AuthenticationState(principal);

			_authStateProviderMock
				.Setup(x => x.GetAuthenticationStateAsync())
				.ReturnsAsync(authState);

			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the app_user_id claim
		/// is an empty string.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WithEmptyAppUserId_ReturnsNull()
		{
			// Arrange
			var claims = new List<Claim>
			{
				new("app_user_id", string.Empty)
			};
			var identity = new ClaimsIdentity(claims, "TestAuth");
			var principal = new ClaimsPrincipal(identity);
			var authState = new AuthenticationState(principal);

			_authStateProviderMock
				.Setup(x => x.GetAuthenticationStateAsync())
				.ReturnsAsync(authState);

			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the identity is authenticated
		/// but the app_user_id claim is missing entirely.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WithMissingAppUserIdClaim_ReturnsNull()
		{
			// Arrange - Authenticated but no app_user_id claim
			var claims = new List<Claim>
			{
				new(ClaimTypes.Email, "test@example.com"),
				new(ClaimTypes.Name, "Test User")
			};
			var identity = new ClaimsIdentity(claims, "TestAuth");
			var principal = new ClaimsPrincipal(identity);
			var authState = new AuthenticationState(principal);

			_authStateProviderMock
				.Setup(x => x.GetAuthenticationStateAsync())
				.ReturnsAsync(authState);

			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		#endregion

		#region Edge Cases - Database State

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns the correct user when multiple users
		/// exist in the database. This ensures proper filtering by user ID.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_MultipleUsersInDatabase_ReturnsCorrectUser()
		{
			// Arrange
			var user1 = await CreateTestUserAsync("google-111", "user1@example.com", "User One");
			var user2 = await CreateTestUserAsync("google-222", "user2@example.com", "User Two");
			var user3 = await CreateTestUserAsync("google-333", "user3@example.com", "User Three");

			SetupAuthenticatedUser(user2.Id);
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.NotNull(result);
			Assert.Equal(user2.Id, result.Id);
			Assert.Equal("User Two", result.DisplayName);
			Assert.Equal("user2@example.com", result.Email);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the user ID is negative.
		/// Negative IDs are invalid and should not match any database records.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WithNegativeUserId_ReturnsNull()
		{
			// Arrange
			SetupAuthenticatedUser(userId: -1);
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the user ID is zero.
		/// Zero is not a valid user ID in the database.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WithZeroUserId_ReturnsNull()
		{
			// Arrange
			SetupAuthenticatedUser(userId: 0);
			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		#endregion

		#region Authentication State Edge Cases

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the ClaimsPrincipal
		/// has no identity (null or empty identities collection).
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WithNullIdentity_ReturnsNull()
		{
			// Arrange
			var principal = new ClaimsPrincipal();
			var authState = new AuthenticationState(principal);

			_authStateProviderMock
				.Setup(x => x.GetAuthenticationStateAsync())
				.ReturnsAsync(authState);

			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		/// <summary>
		/// Verifies that GetCurrentUserAsync returns null when the identity exists
		/// and has claims, but IsAuthenticated is false.
		/// This happens when ClaimsIdentity is created without an authenticationType.
		/// </summary>
		[Fact]
		public async Task GetCurrentUserAsync_WithUnauthenticatedIdentity_ReturnsNull()
		{
			// Arrange - Identity exists but IsAuthenticated is false
			var claims = new List<Claim>
			{
				new("app_user_id", "1")
			};
			// No authenticationType parameter = IsAuthenticated returns false
			var identity = new ClaimsIdentity(claims);
			var principal = new ClaimsPrincipal(identity);
			var authState = new AuthenticationState(principal);

			_authStateProviderMock
				.Setup(x => x.GetAuthenticationStateAsync())
				.ReturnsAsync(authState);

			var service = CreateService();

			// Act
			var result = await service.GetCurrentUserAsync();

			// Assert
			Assert.Null(result);
		}

		#endregion
	}
}