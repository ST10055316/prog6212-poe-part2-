using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContractMonthlyClaimSystem.Migrations
{
    /// <inheritdoc />
    public partial class FinalModelSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    ClaimId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Id = table.Column<int>(type: "int", nullable: false),
                    LecturerId = table.Column<int>(type: "int", nullable: false),
                    HoursWorked = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ClaimPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: true),
                    ApproverRole = table.Column<int>(type: "int", nullable: true),
                    ApprovalComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_Claims_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Claims_Users_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClaimApprovals",
                columns: table => new
                {
                    ApprovalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimId = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimApprovals", x => x.ApprovalId);
                    table.ForeignKey(
                        name: "FK_ClaimApprovals_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "ClaimId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimApprovals_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedDate", "Department", "Email", "Name", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Computer Science", "john.smith@university.edu", "Dr. John Smith", "X5RI9KhpBCAJF46WLJNT7huA0ErP76tvfQAWNxKexpc=", 0, "john.smith" },
                    { 2, new DateTime(2024, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Computer Science", "jane.doe@university.edu", "Prof. Jane Doe", "X5RI9KhpBCAJF46WLJNT7huA0ErP76tvfQAWNxKexpc=", 1, "jane.doe" },
                    { 3, new DateTime(2023, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Computer Science", "mike.johnson@university.edu", "Dr. Mike Johnson", "X5RI9KhpBCAJF46WLJNT7huA0ErP76tvfQAWNxKexpc=", 2, "mike.johnson" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimApprovals_ApproverId",
                table: "ClaimApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimApprovals_ClaimId",
                table: "ClaimApprovals",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ApproverId",
                table: "Claims",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_LecturerId",
                table: "Claims",
                column: "LecturerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimApprovals");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
