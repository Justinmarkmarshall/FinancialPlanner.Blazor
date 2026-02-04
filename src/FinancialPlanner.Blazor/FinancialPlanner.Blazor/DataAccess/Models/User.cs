using System.ComponentModel.DataAnnotations;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string GoogleSubject { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public UserProfile? Profile { get; set; }
        public ICollection<Month> Months { get; set; } = [];
        public ICollection<Income> Incomes { get; set; } = [];
        public ICollection<Expenditure> Expenditures { get; set; } = [];
        public ICollection<BankStatement> BankStatements { get; set; } = [];
        public ICollection<IncomeCategory> IncomeCategories { get; set; } = [];
        public ICollection<ExpenseCategory> ExpenseCategories { get; set; } = [];
        public ICollection<UserSession> Sessions { get; set; } = [];
    }
}