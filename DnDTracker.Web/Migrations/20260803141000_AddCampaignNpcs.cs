using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnDTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignNpcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignNpcs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ancestry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenderPresentation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgeCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Occupation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Appearance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistinctiveFeature = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Personality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mannerism = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Voice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Background = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Motivation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentProblem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestHook = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DangerOrComplication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DmSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignNpcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignNpcs_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignNpcs_CampaignId",
                table: "CampaignNpcs",
                column: "CampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignNpcs");
        }
    }
}
