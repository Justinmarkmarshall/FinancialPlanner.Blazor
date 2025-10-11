using System.ComponentModel.DataAnnotations;

namespace FinancialPlanner.Blazor.DataAccess.Models
{
    public class Month
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = new DateTime();
        public DateTime EndDate { get; set; } = new DateTime();

        public decimal TotalIncome { get; set; }

        public decimal TotalExpenditure { get; set; }

        public decimal Savings { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}
