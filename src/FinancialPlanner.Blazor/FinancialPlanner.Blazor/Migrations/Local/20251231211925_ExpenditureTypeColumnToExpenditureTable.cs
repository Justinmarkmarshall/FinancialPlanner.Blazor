using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialPlanner.Blazor.Migrations
{
    /// <inheritdoc />
    public partial class ExpenditureTypeColumnToExpenditureTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpenseType",
                table: "Expenditures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpenseType",
                table: "Expenditures");
        }
    }
}
