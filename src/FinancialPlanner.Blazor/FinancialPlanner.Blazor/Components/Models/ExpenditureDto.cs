using System.ComponentModel.DataAnnotations;
using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;

namespace FinancialPlanner.Blazor.Components.Models
{
    public class ExpenditureDto : CashflowDto
    {
        // let's have this as the name of the caterofy at this stage since we are just showing this to the UI
        // AND BY NOW IT IS A CONCRETE EXPEDNTIURE WHICH HAS BEEN CORRECTLY CONFIGURED
        public string Category { get; set; } = "";

        public CashflowType ExpenseType { get; set; }

        public int? ExpenseCategoryId { get; set; }
    }
}