using Microsoft.EntityFrameworkCore.Migrations;

public partial class SeedData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Read from embedded resource or file
        var seedSql = File.ReadAllText("seed_data.sql");
        migrationBuilder.Sql(seedSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Optionally truncate tables
    }
}