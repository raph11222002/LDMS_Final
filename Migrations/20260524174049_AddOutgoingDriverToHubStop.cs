using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDMS_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddOutgoingDriverToHubStop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OutgoingDriverId",
                table: "OrderHubStops",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OutgoingDriverLabel",
                table: "OrderHubStops",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHubStops_OutgoingDriverId",
                table: "OrderHubStops",
                column: "OutgoingDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderHubStops_AspNetUsers_OutgoingDriverId",
                table: "OrderHubStops",
                column: "OutgoingDriverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderHubStops_AspNetUsers_OutgoingDriverId",
                table: "OrderHubStops");

            migrationBuilder.DropIndex(
                name: "IX_OrderHubStops_OutgoingDriverId",
                table: "OrderHubStops");

            migrationBuilder.DropColumn(
                name: "OutgoingDriverId",
                table: "OrderHubStops");

            migrationBuilder.DropColumn(
                name: "OutgoingDriverLabel",
                table: "OrderHubStops");
        }
    }
}
