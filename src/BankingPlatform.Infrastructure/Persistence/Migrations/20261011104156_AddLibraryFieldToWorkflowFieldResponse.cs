using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryFieldToWorkflowFieldResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowFieldResponses_WorkflowTaskId_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "LibraryFieldId",
                table: "WorkflowFieldResponses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_LibraryFieldId",
                table: "WorkflowFieldResponses",
                column: "LibraryFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_WorkflowTaskId",
                table: "WorkflowFieldResponses",
                column: "WorkflowTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowFieldResponses_LibraryFields_LibraryFieldId",
                table: "WorkflowFieldResponses",
                column: "LibraryFieldId",
                principalTable: "LibraryFields",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowFieldResponses_LibraryFields_LibraryFieldId",
                table: "WorkflowFieldResponses");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowFieldResponses_LibraryFieldId",
                table: "WorkflowFieldResponses");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowFieldResponses_WorkflowTaskId",
                table: "WorkflowFieldResponses");

            migrationBuilder.DropColumn(
                name: "LibraryFieldId",
                table: "WorkflowFieldResponses");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldResponses_WorkflowTaskId_WorkflowNodeFieldId",
                table: "WorkflowFieldResponses",
                columns: new[] { "WorkflowTaskId", "WorkflowNodeFieldId" },
                unique: true);
        }
    }
}
