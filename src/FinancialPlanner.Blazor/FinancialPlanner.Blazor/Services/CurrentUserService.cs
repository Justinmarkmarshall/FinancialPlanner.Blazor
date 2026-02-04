using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly FinanceDbContext _dbContext;

    private User? _cachedUser;
    private int? _cachedUserId;

    public CurrentUserService(
        AuthenticationStateProvider authenticationStateProvider,
        FinanceDbContext dbContext)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _dbContext = dbContext;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            ClearCache();
            return null;
        }

        var userIdStr = authState.User.FindFirstValue("app_user_id");
        if (!int.TryParse(userIdStr, out var userId))
        {
            ClearCache();
            return null;
        }

        if (_cachedUser != null && _cachedUserId == userId)
            return _cachedUser;

        _cachedUser = await _dbContext.Users
            .Include(u => u.Profile)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        _cachedUserId = userId;
        return _cachedUser;
    }

    public async Task<int?> GetCurrentUserIdAsync()
        => (await GetCurrentUserAsync())?.Id;

    public async Task<bool> IsAuthenticatedAsync()
        => (await GetCurrentUserAsync()) != null;

    public void ClearCache()
    {
        _cachedUser = null;
        _cachedUserId = null;
    }
}

public interface ICurrentUserService
{
    Task<User?> GetCurrentUserAsync();
    Task<int?> GetCurrentUserIdAsync();
    Task<bool> IsAuthenticatedAsync();
    void ClearCache();
}