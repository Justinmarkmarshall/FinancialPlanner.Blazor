using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialPlanner.Blazor.Migrations
{
    /// <inheritdoc />
    public partial class BackfillUsersAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure FK enforcement during backfill
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");

            // 1) Ensure default user exists
            migrationBuilder.Sql(@"
                INSERT INTO Users (GoogleSubject, Email, DisplayName, CreatedUtc)
                SELECT 'local-dev', 'local@financeplanner', 'Local User', CURRENT_TIMESTAMP
                WHERE NOT EXISTS (
                    SELECT 1 FROM Users WHERE GoogleSubject = 'local-dev'
                );
            ");

            // 2) Backfill UserId on existing data
            migrationBuilder.Sql(@"
                UPDATE Months
                SET UserId = (SELECT Id FROM Users WHERE GoogleSubject = 'local-dev')
                WHERE UserId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE Incomes
                SET UserId = (SELECT Id FROM Users WHERE GoogleSubject = 'local-dev')
                WHERE UserId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE Expenditures
                SET UserId = (SELECT Id FROM Users WHERE GoogleSubject = 'local-dev')
                WHERE UserId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE BankStatements
                SET UserId = (SELECT Id FROM Users WHERE GoogleSubject = 'local-dev')
                WHERE UserId IS NULL;
            ");

            // 3) Create default ExpenseCategories for the local user
            migrationBuilder.Sql(@"
                INSERT INTO ExpenseCategories (UserId, Name, SortOrder, IsArchived)
                VALUES
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Groceries', 1, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Utilities', 2, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Housing', 3, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Car', 4, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'PersonalCare', 5, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Home', 6, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Tithing', 7, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Education', 8, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'DebtRepayment', 9, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'DiningOut', 10, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Miscellaneous', 11, 0);
            ");

            // 4) Create default IncomeCategories for the local user
            migrationBuilder.Sql(@"
                INSERT INTO IncomeCategories (UserId, Name, SortOrder, IsArchived)
                VALUES
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Salary', 1, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Lodger', 2, 0),
                    ((SELECT Id FROM Users WHERE GoogleSubject = 'local-dev'), 'Other', 3, 0);
            ");

            // 5) Set a default ExpenseCategoryId for existing Expenditures (Miscellaneous)
            migrationBuilder.Sql(@"
                UPDATE Expenditures
                SET ExpenseCategoryId = (
                    SELECT Id FROM ExpenseCategories 
                    WHERE UserId = (SELECT Id FROM Users WHERE GoogleSubject = 'local-dev')
                      AND Name = 'Miscellaneous'
                )
                WHERE ExpenseCategoryId IS NULL;
            ");

            // 6) Set a default IncomeCategoryId for existing Incomes (Other)
            migrationBuilder.Sql(@"
                UPDATE Incomes
                SET IncomeCategoryId = (
                    SELECT Id FROM IncomeCategories 
                    WHERE UserId = (SELECT Id FROM Users WHERE GoogleSubject = 'local-dev')
                      AND Name = 'Other'
                )
                WHERE IncomeCategoryId IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
