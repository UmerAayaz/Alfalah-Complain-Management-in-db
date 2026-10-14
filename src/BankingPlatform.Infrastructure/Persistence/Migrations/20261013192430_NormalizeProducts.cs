using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // STEP 1: Drop purane FKs (taake data move ho sake)
            // ============================================================
            migrationBuilder.DropForeignKey(
                name: "FK_ComplaintCategories_ComplaintCategories_ParentId",
                table: "ComplaintCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_ComplaintCategories_ProductId",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowDefinitions_ComplaintCategories_ProductId",
                table: "WorkflowDefinitions");

            // ============================================================
            // STEP 2: Products table banao (khali)
            // ============================================================
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ComplaintCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ComplaintCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_Code",
                table: "Products",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            // ============================================================
            // STEP 3: Data copy — purane products ko move karo
            // ============================================================
            migrationBuilder.Sql(@"
                INSERT INTO Products (Id, CategoryId, Code, Name, IsActive, Priority, CreatedAtUtc)
                SELECT Id, ParentId, Code, Name, IsActive, Priority, CreatedAtUtc
                FROM ComplaintCategories
                WHERE ParentId IS NOT NULL;
            ");

            // ============================================================
            // STEP 4: ComplaintCategories se product rows delete karo
            // (ab woh Products table mein hain)
            // ============================================================
            migrationBuilder.Sql(@"
                DELETE FROM ComplaintCategories WHERE ParentId IS NOT NULL;
            ");

            // ============================================================
            // STEP 5: Ab ParentId + Priority columns drop karo
            // ============================================================
            migrationBuilder.DropIndex(
                name: "IX_ComplaintCategories_ParentId",
                table: "ComplaintCategories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ComplaintCategories");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ComplaintCategories");

            // ============================================================
            // STEP 6: Naye FKs — ab Products table ko point karein
            // ============================================================
            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_Products_ProductId",
                table: "Complaints",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowDefinitions_Products_ProductId",
                table: "WorkflowDefinitions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // STEP 1: Drop naye FKs
            // ============================================================
            migrationBuilder.DropForeignKey(
                name: "FK_Complaints_Products_ProductId",
                table: "Complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowDefinitions_Products_ProductId",
                table: "WorkflowDefinitions");

            // ============================================================
            // STEP 2: ParentId + Priority columns wapis add karo
            // ============================================================
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
                name: "IX_ComplaintCategories_ParentId",
                table: "ComplaintCategories",
                column: "ParentId");

            // ============================================================
            // STEP 3: Data wapis copy karo — Products se ComplaintCategories
            // ============================================================
            migrationBuilder.Sql(@"
                INSERT INTO ComplaintCategories (Id, DepartmentId, Code, Name, IsActive, ParentId, Priority, CreatedAtUtc)
                SELECT p.Id, c.DepartmentId, p.Code, p.Name, p.IsActive, p.CategoryId, p.Priority, p.CreatedAtUtc
                FROM Products p
                INNER JOIN ComplaintCategories c ON c.Id = p.CategoryId;
            ");

            // ============================================================
            // STEP 4: Self-ref FK wapis add karo
            // ============================================================
            migrationBuilder.AddForeignKey(
                name: "FK_ComplaintCategories_ComplaintCategories_ParentId",
                table: "ComplaintCategories",
                column: "ParentId",
                principalTable: "ComplaintCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ============================================================
            // STEP 5: Products table delete karo
            // ============================================================
            migrationBuilder.DropTable(
                name: "Products");

            // ============================================================
            // STEP 6: Purane FKs wapis add karo
            // ============================================================
            migrationBuilder.AddForeignKey(
                name: "FK_Complaints_ComplaintCategories_ProductId",
                table: "Complaints",
                column: "ProductId",
                principalTable: "ComplaintCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowDefinitions_ComplaintCategories_ProductId",
                table: "WorkflowDefinitions",
                column: "ProductId",
                principalTable: "ComplaintCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}