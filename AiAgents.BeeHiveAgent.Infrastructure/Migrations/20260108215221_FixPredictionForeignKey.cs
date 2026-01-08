using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgents.BeeHiveAgent.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPredictionForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_ImageSamples_HiveImageSampleId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_HiveImageSampleId",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "HiveImageSampleId",
                table: "Predictions");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_SampleId",
                table: "Predictions",
                column: "SampleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_ImageSamples_SampleId",
                table: "Predictions",
                column: "SampleId",
                principalTable: "ImageSamples",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_ImageSamples_SampleId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_SampleId",
                table: "Predictions");

            migrationBuilder.AddColumn<Guid>(
                name: "HiveImageSampleId",
                table: "Predictions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_HiveImageSampleId",
                table: "Predictions",
                column: "HiveImageSampleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_ImageSamples_HiveImageSampleId",
                table: "Predictions",
                column: "HiveImageSampleId",
                principalTable: "ImageSamples",
                principalColumn: "Id");
        }
    }
}
