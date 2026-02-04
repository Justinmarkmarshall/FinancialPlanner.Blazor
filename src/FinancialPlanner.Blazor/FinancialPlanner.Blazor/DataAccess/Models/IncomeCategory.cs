using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class IncomeCategory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsArchived { get; set; }

        // Navigation property
        public ICollection<Income> Incomes { get; set; } = [];
    }
}
