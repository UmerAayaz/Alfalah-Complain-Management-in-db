using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductsAndPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultPriority",
                table: "WorkflowDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AutoReference",
                table: "Complaints",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Complaints",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "ComplaintCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ComplaintCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ProductId",
                table: "Complaints",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintCategories_ParentId",
                table: "ComplaintCategories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComplaintCategories_ComplaintCategories_ParentId",
                table: "ComplaintCategories",
                column: "ParentId",
                principalTable: "ComplaintCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_ComplaintCategories_ProductId",
                table: "Complaints",
                column: "ProductId",
                principalTable: "ComplaintCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComplaintCategories_ComplaintCategories_ParentId",
                table: "ComplaintCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_ComplaintCategories_ProductId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_Complaints_ProductId",
                table: "Complaints");

            migrationBuilder.DropIndex(
                name: "IX_ComplaintCategories_ParentId",
                table: "ComplaintCategories");

            migrationBuilder.DropColumn(
                name: "DefaultPriority",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "AutoReference",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ComplaintCategories");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ComplaintCategories");
        }
    }
}
