using static FinancialPlanner.Blazor.DataAccess.Helpers.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinancialPlanner.Blazor.Components.Models
{
    public class IncomeDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public bool Recurring { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public IncomeCategory Category { get; set; } = IncomeCategory.Other;
    }
}
