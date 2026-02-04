using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialPlanner.Blazor.Migrations
{
    /// <inheritdoc />
    public partial class ActualColumnsToMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalProjectedIncome",
                table: "Months",
                newName: "ProjectedTotalIncome");

            migrationBuilder.RenameColumn(
                name: "TotalProjectedExpenditure",
                table: "Months",
                newName: "ProjectedTotalExpenditure");

            migrationBuilder.RenameColumn(
                name: "TotalActualIncome",
                table: "Months",
                newName: "ActualTotalIncome");

            migrationBuilder.RenameColumn(
                name: "TotalActualExpenditure",
                table: "Months",
                newName: "ActualTotalExpenditure");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProjectedTotalIncome",
                table: "Months",
                newName: "TotalProjectedIncome");

            migrationBuilder.RenameColumn(
                name: "ProjectedTotalExpenditure",
                table: "Months",
                newName: "TotalProjectedExpenditure");

            migrationBuilder.RenameColumn(
                name: "ActualTotalIncome",
                table: "Months",
                newName: "TotalActualIncome");

            migrationBuilder.RenameColumn(
                name: "ActualTotalExpenditure",
                table: "Months",
                newName: "TotalActualExpenditure");
        }
    }
}
