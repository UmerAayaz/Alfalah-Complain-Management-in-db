using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkflowNodeFieldIdFromResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowFieldResponses_WorkflowNodeFields_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowFieldResponses_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses");

            migrationBuilder.DropColumn(
                name: "WorkflowNodeFieldId",
                table: "WorkflowFieldResponses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                column: "WorkflowNodeFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowFieldResponses_WorkflowNodeFields_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                column: "WorkflowNodeFieldId",
                principalTable: "WorkflowNodeFields",
                principalColumn: "Id");
        }
    }
}
