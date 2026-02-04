namespace FinancialPlanner.Blazor.Components.Models
{
    public class HistoryDto
    {
        public List<MonthDto> Months { get; set; } = new List<MonthDto>();

        public List<BankStatementDto> BankStatements { get; set; } = new List<BankStatementDto>();
    }
}
