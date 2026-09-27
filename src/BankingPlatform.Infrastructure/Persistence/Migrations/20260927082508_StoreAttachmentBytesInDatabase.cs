using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreAttachmentBytesInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "WorkflowFieldAttachments");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "WorkflowFieldAttachments");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "WorkflowFieldAttachments",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "WorkflowFieldAttachments",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Sha256Hash",
                table: "WorkflowFieldAttachments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "WorkflowFieldAttachments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadedByUserId",
                table: "WorkflowFieldAttachments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowFieldAttachments_UploadedByUserId",
                table: "WorkflowFieldAttachments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowFieldAttachments_UploadedByUserId",
                table: "WorkflowFieldAttachments");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "WorkflowFieldAttachments");

            migrationBuilder.DropColumn(
                name: "Sha256Hash",
                table: "WorkflowFieldAttachments");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "WorkflowFieldAttachments");

            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "WorkflowFieldAttachments");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "WorkflowFieldAttachments",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "WorkflowFieldAttachments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "WorkflowFieldAttachments",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
