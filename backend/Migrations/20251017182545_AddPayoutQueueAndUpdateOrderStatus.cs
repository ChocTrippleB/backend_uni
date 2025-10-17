using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutQueueAndUpdateOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayoutQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    SellerId = table.Column<int>(type: "integer", nullable: false),
                    SellerRecipientCode = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledPayoutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TransferReference = table.Column<string>(type: "text", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutQueue_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayoutQueue_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutQueue_OrderId",
                table: "PayoutQueue",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutQueue_SellerId",
                table: "PayoutQueue",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutQueue_Status_ScheduledPayoutDate",
                table: "PayoutQueue",
                columns: new[] { "Status", "ScheduledPayoutDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoutQueue");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");
        }
    }
}
