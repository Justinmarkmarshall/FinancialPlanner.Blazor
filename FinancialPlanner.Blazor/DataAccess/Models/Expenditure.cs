using FinancialPlanner.Blazor.DataAccess.Helpers;
using System.ComponentModel.DataAnnotations;
using static FinancialPlanner.Blazor.DataAccess.Helpers.Enums;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class Expenditure
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public Category Category { get; set; } = Category.Miscellaneous;

        public bool Recurring { get; set; }

        public DateTime StartDate { get; set; } = new DateTime();

        public DateTime EndDate { get; set; } = new DateTime();

    }
}
