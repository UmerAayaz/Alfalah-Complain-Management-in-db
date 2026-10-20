using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropWorkflowNodeFieldAttachmentConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowNodeFieldAttachmentConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowNodeFieldAttachmentConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowNodeFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllowMultiple = table.Column<bool>(type: "bit", nullable: true),
                    AllowedFileTypesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxFileSizeMb = table.Column<int>(type: "int", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowNodeFieldAttachmentConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowNodeFieldAttachmentConfigs_WorkflowNodeFields_WorkflowNodeFieldId",
                        column: x => x.WorkflowNodeFieldId,
                        principalTable: "WorkflowNodeFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowNodeFieldAttachmentConfigs_WorkflowNodeFieldId",
                table: "WorkflowNodeFieldAttachmentConfigs",
                column: "WorkflowNodeFieldId",
                unique: true);
        }
    }
}
