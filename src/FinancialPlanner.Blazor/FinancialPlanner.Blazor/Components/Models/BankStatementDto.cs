namespace FinancialPlanner.Blazor.Components.Models
{
    public class BankStatementDto
    {
        public int Id { get; set; }
        public MonthDto Month { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal TotalIncome { get; set; }
        public decimal TotalOutgoing { get; set; }
    }
}
