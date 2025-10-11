using FinancialPlanner.Blazor.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlanner.Blazor.DataAccess
{
    public class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
    {
        public DbSet<Month> Months { get; set; } = null!;
        public DbSet<Expenditure> Expenditures { get; set; } = null!;
        public DbSet<Income> Incomes { get; set; } = null!;
    }
}
