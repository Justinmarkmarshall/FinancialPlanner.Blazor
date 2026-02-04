using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class UserProfile
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "GBP";

        public int PaydayDayOfMonth { get; set; } = 1;

        public decimal DefaultSavingsTarget { get; set; }

        [MaxLength(20)]
        public string Locale { get; set; } = "en-GB";

        // Navigation property
        public User User { get; set; } = null!;
    }
}