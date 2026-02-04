using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class UserSession
    {
        [Key]
        public Guid SessionId { get; set; } = Guid.NewGuid();

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresUtc { get; set; }

        public DateTime? RevokedUtc { get; set; }

        public DateTime? LastSeenUtc { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        [NotMapped]
        public bool IsValid => RevokedUtc == null && ExpiresUtc > DateTime.UtcNow;
    }
}
