using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgents.BeeHiveAgent.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageSamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewGoldSinceLastTrain = table.Column<int>(type: "int", nullable: false),
                    RetrainGoldThreshold = table.Column<int>(type: "int", nullable: false),
                    AutoThresholdHigh = table.Column<float>(type: "real", nullable: false),
                    AutoThresholdLow = table.Column<float>(type: "real", nullable: false),
                    IsRetrainEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ActiveModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    PredictedLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HiveImageSampleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predictions_ImageSamples_HiveImageSampleId",
                        column: x => x.HiveImageSampleId,
                        principalTable: "ImageSamples",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "ActiveModelVersionId", "AutoThresholdHigh", "AutoThresholdLow", "IsRetrainEnabled", "NewGoldSinceLastTrain", "RetrainGoldThreshold" },
                values: new object[] { 1, null, 0.9f, 0.15f, true, 0, 50 });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_HiveImageSampleId",
                table: "Predictions",
                column: "HiveImageSampleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "ImageSamples");
        }
    }
}
