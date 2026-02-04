using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;

namespace FinancialPlanner.Blazor.Components.Models
{
    // base class for IncomeDto and ExpenditureDto
    public abstract class CashflowDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        /// <summary>
        /// Changing this to be payment dates, one off payments will use this date only, recurring payments will use the day component of this date to determine when in the month it occurs.
        /// </summary>
        public DateTime PaymentDate { get; set; }

        public bool Recurring { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int UserId { get; set; }
    }
}
