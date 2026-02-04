using FinancialPlanner.Blazor.DataAccess;
using FinancialPlanner.Blazor.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlanner.Blazor.Services
{
    public class SessionService : ISessionService
    {
        private readonly FinanceDbContext _dbContext;

        public SessionService(FinanceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserSession> CreateSessionAsync(int userId, string? ipAddress, string? userAgent, TimeSpan? expiry = null)
        {
            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromDays(7)),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _dbContext.UserSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return session;
        }

        public async Task<UserSession?> ValidateSessionAsync(Guid sessionId)
        {
            var now = DateTime.UtcNow;
            return await _dbContext.UserSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.SessionId == sessionId &&
                    s.RevokedUtc == null &&
                    s.ExpiresUtc > now);
        }

        public async Task<bool> TouchSessionAsync(Guid sessionId, int userId)
        {
            var now = DateTime.UtcNow;
            var updated = await _dbContext.UserSessions
                .Where(s => s.SessionId == sessionId && s.UserId == userId && s.RevokedUtc == null && s.ExpiresUtc > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.LastSeenUtc, now));

            return updated == 1;
        }


        public async Task<bool> RevokeSessionAsync(Guid sessionId)
        {
            var session = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                return false;
            }

            session.RevokedUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public Task RevokeAllSessionsAsync(int userId)
        {
            var now = DateTime.UtcNow;
            return _dbContext.UserSessions
                .Where(s => s.UserId == userId && s.RevokedUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.RevokedUtc, now));
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _dbContext.UserSessions
                .Where(s => s.ExpiresUtc < DateTime.UtcNow || s.RevokedUtc != null)
                .Where(s => s.CreatedUtc < DateTime.UtcNow.AddDays(-30)) // Keep for audit for 30 days
                .ToListAsync();

            _dbContext.UserSessions.RemoveRange(expiredSessions);
            await _dbContext.SaveChangesAsync();
        }
    }

    public interface ISessionService
    {
        Task<UserSession> CreateSessionAsync(int userId, string? ipAddress, string? userAgent, TimeSpan? expiry = null);
        Task<UserSession?> ValidateSessionAsync(Guid sessionId);
        Task<bool> RevokeSessionAsync(Guid sessionId);
        Task RevokeAllSessionsAsync(int userId);
        Task<bool> TouchSessionAsync(Guid sessionId, int userId);
        Task CleanupExpiredSessionsAsync();
    }
}