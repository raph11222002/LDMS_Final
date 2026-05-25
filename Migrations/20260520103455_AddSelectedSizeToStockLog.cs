using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDMS_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedSizeToStockLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedSize",
                table: "ProductStocks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SelectedSize",
                table: "ProductStockLogs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedSize",
                table: "ProductStocks");

            migrationBuilder.DropColumn(
                name: "SelectedSize",
                table: "ProductStockLogs");
        }
    }
}
