using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialPlanner.Blazor.Migrations
{
    /// <inheritdoc />
    public partial class UnLinkExpAndIncToMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenditures_Months_MonthId",
                table: "Expenditures");

            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_Months_MonthId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_MonthId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Expenditures_MonthId",
                table: "Expenditures");

            migrationBuilder.DropColumn(
                name: "MonthId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "MonthId",
                table: "Expenditures");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonthId",
                table: "Incomes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthId",
                table: "Expenditures",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_MonthId",
                table: "Incomes",
                column: "MonthId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenditures_MonthId",
                table: "Expenditures",
                column: "MonthId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenditures_Months_MonthId",
                table: "Expenditures",
                column: "MonthId",
                principalTable: "Months",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_Months_MonthId",
                table: "Incomes",
                column: "MonthId",
                principalTable: "Months",
                principalColumn: "Id");
        }
    }
}
