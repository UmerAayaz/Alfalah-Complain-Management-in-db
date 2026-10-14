using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductToWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "WorkflowDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_ProductId",
                table: "WorkflowDefinitions",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowDefinitions_ComplaintCategories_ProductId",
                table: "WorkflowDefinitions",
                column: "ProductId",
                principalTable: "ComplaintCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowDefinitions_ComplaintCategories_ProductId",
                table: "WorkflowDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinitions_ProductId",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "WorkflowDefinitions");
        }
    }
}
