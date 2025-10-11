namespace FinancialPlanner.Blazor.Components.Models
{
    public class MonthDto
    {
        public int MyProperty { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime StartDate { get; set; } = new DateTime();

        public DateTime EndDate { get; set; } = new DateTime();

        public int Year { get; set; }
        
        public decimal TotalIncome { get; set; }

        public decimal TotalExpenditure { get; set; }

        public List<ExpenditureDto> Expenditures { get; set; } = new List<ExpenditureDto>();

        public decimal Savings { get; set; }
    }
}
