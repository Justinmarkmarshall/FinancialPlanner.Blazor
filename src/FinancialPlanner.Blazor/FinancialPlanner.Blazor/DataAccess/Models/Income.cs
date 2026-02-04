using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class Income
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? MonthId { get; set; }

        [ForeignKey("MonthId")]
        public Month? Month { get; set; }

        public int? IncomeCategoryId { get; set; }

        [ForeignKey("IncomeCategoryId")]
        public IncomeCategory? Category { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [Column("Date")]
        public DateTime PaymentDate { get; set; }

        public bool Recurring { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int MyProperty { get; set; }

        public CashflowType IncomeType { get; set; } = CashflowType.Projected;
    }
}