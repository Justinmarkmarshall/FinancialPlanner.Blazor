using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class Month
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal ProjectedTotalIncome { get; set; }
        public decimal ProjectedTotalExpenditure { get; set; }
        public decimal ProjectedSavings { get; set; }

        public string Notes { get; set; } = string.Empty;

        public decimal ActualTotalIncome { get; set; }
        public decimal ActualTotalExpenditure { get; set; }
        public decimal ActualSavings { get; set; }

        // Navigation properties
        public ICollection<BankStatement> BankStatements { get; set; } = [];
        public ICollection<Income> Incomes { get; set; } = [];
        public ICollection<Expenditure> Expenditures { get; set; } = [];
    }
}