using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinancialPlanner.Blazor.Components.Models
{
    public class IncomeDto : CashflowDto
    {
        // let's have this as the name of the category at this stage since we are just showing this to the UI
        public string Category { get; set; }

        public CashflowType IncomeType { get; set; }
        public int? IncomeCategoryId { get; set; }
    }
}
