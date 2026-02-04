using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialPlanner.Blazor.Migrations
{
    /// <inheritdoc />
    public partial class ActualIncomeExpenditureSavingsToMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalIncome",
                table: "Months",
                newName: "TotalProjectedIncome");

            migrationBuilder.RenameColumn(
                name: "TotalExpenditure",
                table: "Months",
                newName: "TotalProjectedExpenditure");

            migrationBuilder.RenameColumn(
                name: "Savings",
                table: "Months",
                newName: "TotalActualIncome");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualSavings",
                table: "Months",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectedSavings",
                table: "Months",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalActualExpenditure",
                table: "Months",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualSavings",
                table: "Months");

            migrationBuilder.DropColumn(
                name: "ProjectedSavings",
                table: "Months");

            migrationBuilder.DropColumn(
                name: "TotalActualExpenditure",
                table: "Months");

            migrationBuilder.RenameColumn(
                name: "TotalProjectedIncome",
                table: "Months",
                newName: "TotalIncome");

            migrationBuilder.RenameColumn(
                name: "TotalProjectedExpenditure",
                table: "Months",
                newName: "TotalExpenditure");

            migrationBuilder.RenameColumn(
                name: "TotalActualIncome",
                table: "Months",
                newName: "Savings");
        }
    }
}
