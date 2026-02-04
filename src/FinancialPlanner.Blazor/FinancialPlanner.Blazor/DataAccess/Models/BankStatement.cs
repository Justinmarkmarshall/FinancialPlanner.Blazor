using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class BankStatement
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int MonthId { get; set; }

        [ForeignKey("MonthId")]
        public Month Month { get; set; } = null!;

        public string Description { get; set; } = string.Empty;
        public decimal TotalIncome { get; set; }
        public decimal TotalOutgoing { get; set; }
    }
}
