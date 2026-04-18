using System;
using System.Collections.Generic;

namespace FinancialPlanner.Blazor.Components.Models
{
    public class MonthDto
    {
        public int Id { get; set; } 

        public string Name { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = new DateTime();

        public DateTime EndDate { get; set; } = new DateTime();

        public int Year { get; set; }
        
        public decimal ProjectedTotalIncome { get; set; }

        public List<IncomeDto> ProjectedIncomes { get; set; } = new List<IncomeDto>();

        public decimal ProjectedTotalExpenditure { get; set; }

        public List<ExpenditureDto> ProjectedExpenditures { get; set; } = new List<ExpenditureDto>();

        public decimal ProjectedSavings { get; set; }

        public List<IncomeDto> ActualIncomes { get; set; } = new List<IncomeDto>();

        public List<ExpenditureDto> ActualExpenditures { get; set; } = new List<ExpenditureDto>();

        public decimal ActualTotalExpenditure { get; set; }

        public decimal ActualTotalIncome { get; set; }

        // Stored monthly actual savings (can be loaded from DB)
        public decimal ActualSavings { get; set; }

        // Legacy field (kept for existing code that references it)
        public decimal RunningTotalSavings { get; set; }

        public string Notes { get; set; } = string.Empty;

        // Computed: projected running total (previous actual running total + projected savings)
        public decimal RunningTotalProjected { get; set; }

        // Computed / editable: actual running total
        public decimal RunningTotalActual { get; set; }

        // Mark if user edited the RunningTotalActual for this month
        public bool IsRunningTotalEdited { get; set; } = false;
    }
}
