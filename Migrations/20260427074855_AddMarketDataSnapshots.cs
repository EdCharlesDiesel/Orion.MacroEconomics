using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orion.API.TradingEconomics.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketDataSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "market_data_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ToUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IngestedAtUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_market_data_snapshots_Provider_DataType_Symbol_FromUtc_ToUtc",
                table: "market_data_snapshots",
                columns: new[] { "Provider", "DataType", "Symbol", "FromUtc", "ToUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_data_snapshots");
        }
    }
}
