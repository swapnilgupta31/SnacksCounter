using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackCounter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeRateSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ToCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    ReportMonth = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MonthlyBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HomeOfficeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOfficeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PayrollCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    IsVolunteer = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOnLeave = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPresentToday = table.Column<bool>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastAssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Offices_CurrentOfficeId",
                        column: x => x.CurrentOfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Offices_HomeOfficeId",
                        column: x => x.HomeOfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SnackBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OfficeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AvailabilityStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    LastRefilledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnackBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnackBoxes_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefillRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SnackBoxId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfficeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedVolunteerId = table.Column<int>(type: "INTEGER", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReportedByEmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OfferedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OfferExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignmentDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefillRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefillRequests_Employees_AssignedVolunteerId",
                        column: x => x.AssignedVolunteerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RefillRequests_Employees_ReportedByEmployeeId",
                        column: x => x.ReportedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefillRequests_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefillRequests_SnackBoxes_SnackBoxId",
                        column: x => x.SnackBoxId,
                        principalTable: "SnackBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefillRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    OfficeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    BudgetMonth = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetReservations_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetReservations_RefillRequests_RefillRequestId",
                        column: x => x.RefillRequestId,
                        principalTable: "RefillRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipientEmployeeId = table.Column<int>(type: "INTEGER", nullable: true),
                    RefillRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Employees_RecipientEmployeeId",
                        column: x => x.RecipientEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_RefillRequests_RefillRequestId",
                        column: x => x.RefillRequestId,
                        principalTable: "RefillRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefillRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceiptReference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsOverBudget = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_RefillRequests_RefillRequestId",
                        column: x => x.RefillRequestId,
                        principalTable: "RefillRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReimbursementClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefillRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalClaimId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReimbursementClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReimbursementClaims_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReimbursementClaims_RefillRequests_RefillRequestId",
                        column: x => x.RefillRequestId,
                        principalTable: "RefillRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetReservations_OfficeId",
                table: "BudgetReservations",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetReservations_RefillRequestId",
                table: "BudgetReservations",
                column: "RefillRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CurrentOfficeId",
                table: "Employees",
                column: "CurrentOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_HomeOfficeId",
                table: "Employees",
                column: "HomeOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientEmployeeId",
                table: "Notifications",
                column: "RecipientEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RefillRequestId",
                table: "Notifications",
                column: "RefillRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_EmployeeId",
                table: "Purchases",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_RefillRequestId",
                table: "Purchases",
                column: "RefillRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefillRequest_SnackBoxId",
                table: "RefillRequests",
                column: "SnackBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_RefillRequests_AssignedVolunteerId",
                table: "RefillRequests",
                column: "AssignedVolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefillRequests_OfficeId",
                table: "RefillRequests",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefillRequests_ReportedByEmployeeId",
                table: "RefillRequests",
                column: "ReportedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_EmployeeId",
                table: "ReimbursementClaims",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_IdempotencyKey",
                table: "ReimbursementClaims",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_RefillRequestId",
                table: "ReimbursementClaims",
                column: "RefillRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SnackBoxes_OfficeId",
                table: "SnackBoxes",
                column: "OfficeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetReservations");

            migrationBuilder.DropTable(
                name: "ExchangeRateSnapshots");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "ReimbursementClaims");

            migrationBuilder.DropTable(
                name: "RefillRequests");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "SnackBoxes");

            migrationBuilder.DropTable(
                name: "Offices");
        }
    }
}
