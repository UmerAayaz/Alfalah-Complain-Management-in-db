using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkflowTaskIdAndFieldIdFromComplaintAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "ComplaintAttachments");

            migrationBuilder.DropColumn(
                name: "WorkflowTaskId",
                table: "ComplaintAttachments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FieldId",
                table: "ComplaintAttachments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowTaskId",
                table: "ComplaintAttachments",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
