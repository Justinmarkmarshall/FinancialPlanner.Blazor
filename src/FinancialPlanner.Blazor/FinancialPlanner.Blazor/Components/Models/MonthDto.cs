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

        public decimal ActualSavings { get; set; }

        public decimal RunningTotalSavings { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}
