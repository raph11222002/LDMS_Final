using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDMS_Final.Migrations
{
    /// <inheritdoc />
    public partial class LogisticRouteV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderHubStops_AspNetUsers_HubStaffId",
                table: "OrderHubStops");

            migrationBuilder.DropIndex(
                name: "IX_OrderHubStops_HubStaffId",
                table: "OrderHubStops");

            migrationBuilder.DropColumn(
                name: "HubStaffId",
                table: "OrderHubStops");

            migrationBuilder.DropColumn(
                name: "ScanNote",
                table: "OrderHubStops");

            migrationBuilder.RenameColumn(
                name: "StopLabel",
                table: "OrderHubStops",
                newName: "HubLabel");

            migrationBuilder.RenameColumn(
                name: "DepartedAt",
                table: "OrderHubStops",
                newName: "DepartedFromPrevAt");

            migrationBuilder.AddColumn<bool>(
                name: "IsVisibleToBuyer",
                table: "OrderStatusLogs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Hub",
                table: "OrderHubStops",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisibleToBuyer",
                table: "OrderStatusLogs");

            migrationBuilder.RenameColumn(
                name: "HubLabel",
                table: "OrderHubStops",
                newName: "StopLabel");

            migrationBuilder.RenameColumn(
                name: "DepartedFromPrevAt",
                table: "OrderHubStops",
                newName: "DepartedAt");

            migrationBuilder.AlterColumn<int>(
                name: "Hub",
                table: "OrderHubStops",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "HubStaffId",
                table: "OrderHubStops",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ScanNote",
                table: "OrderHubStops",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHubStops_HubStaffId",
                table: "OrderHubStops",
                column: "HubStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderHubStops_AspNetUsers_HubStaffId",
                table: "OrderHubStops",
                column: "HubStaffId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
