using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAfterAttachmentRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowNodeFields_WorkflowNodes_WorkflowNodeId",
                table: "WorkflowNodeFields");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowNodeFields_WorkflowNodes_WorkflowNodeId",
                table: "WorkflowNodeFields",
                column: "WorkflowNodeId",
                principalTable: "WorkflowNodes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowNodeFields_WorkflowNodes_WorkflowNodeId",
                table: "WorkflowNodeFields");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowNodeFields_WorkflowNodes_WorkflowNodeId",
                table: "WorkflowNodeFields",
                column: "WorkflowNodeId",
                principalTable: "WorkflowNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
