using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    public partial class SeedProducts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ALFA MALL
            migrationBuilder.Sql(@"
                DECLARE @deptId UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
                DECLARE @catId UNIQUEIDENTIFIER = (SELECT Id FROM ComplaintCategories WHERE Code = 'ALFA_MALL');
                IF @catId IS NOT NULL
                BEGIN
                    INSERT INTO ComplaintCategories (Id, DepartmentId, Code, Name, IsActive, ParentId, Priority, CreatedAtUtc)
                    VALUES 
                      (NEWID(), @deptId, 'AM-VOUCHER', 'Voucher Issue',       1, @catId, 1, GETUTCDATE()),
                      (NEWID(), @deptId, 'AM-POINTS',  'Points Not Credited', 1, @catId, 2, GETUTCDATE()),
                      (NEWID(), @deptId, 'AM-REFUND',  'Refund Delayed',      1, @catId, 2, GETUTCDATE()),
                      (NEWID(), @deptId, 'AM-PRODUCT', 'Product Quality',     1, @catId, 1, GETUTCDATE());
                END
            ");

            // ATM
            migrationBuilder.Sql(@"
                DECLARE @deptId UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
                DECLARE @catId UNIQUEIDENTIFIER = (SELECT Id FROM ComplaintCategories WHERE Code = 'ATM');
                IF @catId IS NOT NULL
                BEGIN
                    INSERT INTO ComplaintCategories (Id, DepartmentId, Code, Name, IsActive, ParentId, Priority, CreatedAtUtc)
                    VALUES 
                      (NEWID(), @deptId, 'ATM-CASH', 'Cash Not Dispensed', 1, @catId, 3, GETUTCDATE()),
                      (NEWID(), @deptId, 'ATM-CARD', 'Card Retained',      1, @catId, 2, GETUTCDATE()),
                      (NEWID(), @deptId, 'ATM-BAL',  'Wrong Balance',      1, @catId, 2, GETUTCDATE());
                END
            ");

            // TRANSACTION
            migrationBuilder.Sql(@"
                DECLARE @deptId UNIQUEIDENTIFIER = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA';
                DECLARE @catId UNIQUEIDENTIFIER = (SELECT Id FROM ComplaintCategories WHERE Code = 'TRANSACTION');
                IF @catId IS NOT NULL
                BEGIN
                    INSERT INTO ComplaintCategories (Id, DepartmentId, Code, Name, IsActive, ParentId, Priority, CreatedAtUtc)
                    VALUES 
                      (NEWID(), @deptId, 'TXN-FAILED',   'Failed Transaction',  1, @catId, 3, GETUTCDATE()),
                      (NEWID(), @deptId, 'TXN-PENDING',  'Pending Transaction', 1, @catId, 2, GETUTCDATE()),
                      (NEWID(), @deptId, 'TXN-REVERSED', 'Reversal Issue',      1, @catId, 2, GETUTCDATE());
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ComplaintCategories 
                WHERE Code IN (
                    'AM-VOUCHER', 'AM-POINTS', 'AM-REFUND', 'AM-PRODUCT',
                    'ATM-CASH', 'ATM-CARD', 'ATM-BAL',
                    'TXN-FAILED', 'TXN-PENDING', 'TXN-REVERSED'
                );
            ");
        }
    }
}