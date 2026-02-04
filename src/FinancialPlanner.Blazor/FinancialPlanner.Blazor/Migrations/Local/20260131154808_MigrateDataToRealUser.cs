using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialPlanner.Blazor.Migrations
{
    /// <inheritdoc />
    public partial class MigrateDataToRealUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reassign all data from local-dev user to real Google user
            var tables = new[] { "Months", "Incomes", "Expenditures", "BankStatements",
                                  "ExpenseCategories", "IncomeCategories" };

            foreach (var table in tables)
            {
                migrationBuilder.Sql($@"
                    UPDATE {table}
                    SET UserId = (SELECT Id FROM Users WHERE Email = 'justinmarkark@gmail.com')
                    WHERE UserId = (SELECT Id FROM Users WHERE GoogleSubject = 'local-dev');
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty - this is a one-way data migration
            // Rolling back would reassign data away from the real user, which is not desired
        }
    }
}
