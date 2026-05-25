using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LDMS_Final.Migrations
{
    /// <inheritdoc />
    public partial class AddLogisticRouteAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedHub",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecipientUserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActionUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderRouteAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    AssignedByStaffId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolvedLatitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    ResolvedLongitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    ResolvedAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstimatedDistanceKm = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRouteAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderRouteAssignments_AspNetUsers_AssignedByStaffId",
                        column: x => x.AssignedByStaffId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderRouteAssignments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderHubStops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderRouteAssignmentId = table.Column<int>(type: "int", nullable: false),
                    StopOrder = table.Column<int>(type: "int", nullable: false),
                    Hub = table.Column<int>(type: "int", nullable: true),
                    StopLabel = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedDriverId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DriverLabel = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HubStaffId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StopStatus = table.Column<int>(type: "int", nullable: false),
                    DepartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ScanNote = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderHubStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderHubStops_AspNetUsers_AssignedDriverId",
                        column: x => x.AssignedDriverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderHubStops_AspNetUsers_HubStaffId",
                        column: x => x.HubStaffId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderHubStops_OrderRouteAssignments_OrderRouteAssignmentId",
                        column: x => x.OrderRouteAssignmentId,
                        principalTable: "OrderRouteAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId",
                table: "Notifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHubStops_AssignedDriverId",
                table: "OrderHubStops",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHubStops_HubStaffId",
                table: "OrderHubStops",
                column: "HubStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHubStops_OrderRouteAssignmentId",
                table: "OrderHubStops",
                column: "OrderRouteAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteAssignments_AssignedByStaffId",
                table: "OrderRouteAssignments",
                column: "AssignedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRouteAssignments_OrderId",
                table: "OrderRouteAssignments",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OrderHubStops");

            migrationBuilder.DropTable(
                name: "OrderRouteAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedHub",
                table: "AspNetUsers");
        }
    }
}
