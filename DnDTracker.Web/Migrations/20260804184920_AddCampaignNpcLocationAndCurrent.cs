using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnDTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignNpcLocationAndCurrent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCurrent",
                table: "CampaignNpcs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CampaignNpcs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "CampaignNpcs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCurrent",
                table: "CampaignNpcs");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CampaignNpcs");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "CampaignNpcs");
        }
    }
}
