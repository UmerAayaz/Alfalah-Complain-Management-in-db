using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReferenceAutoGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoReference",
                table: "Complaints");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerReference",
                table: "Complaints",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CustomerReference",
                table: "Complaints",
                column: "CustomerReference",
                unique: true,
                filter: "[CustomerReference] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Complaints_CustomerReference",
                table: "Complaints");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerReference",
                table: "Complaints",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoReference",
                table: "Complaints",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
