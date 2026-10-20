using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFoundSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Notifications, Conversations and ChatMessages tables (and Items.RejectionReason)
            // already exist in the database, so this migration ONLY adds the soft-delete columns.
            // Each statement checks first, so it is safe even if a column was already added
            // by the earlier failed attempt.

            // ===== Items =====
            migrationBuilder.Sql(@"
IF COL_LENGTH('Items', 'DeletedAt') IS NULL
    ALTER TABLE [Items] ADD [DeletedAt] datetime2 NULL;");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Items', 'DeletedByUserID') IS NULL
    ALTER TABLE [Items] ADD [DeletedByUserID] int NULL;");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Items', 'IsDeleted') IS NULL
    ALTER TABLE [Items] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_Items_IsDeleted] DEFAULT CAST(0 AS bit);");

            // ===== Notifications =====
            migrationBuilder.Sql(@"
IF COL_LENGTH('Notifications', 'DeletedAt') IS NULL
    ALTER TABLE [Notifications] ADD [DeletedAt] datetime2 NULL;");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Notifications', 'DeletedByUserID') IS NULL
    ALTER TABLE [Notifications] ADD [DeletedByUserID] int NULL;");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Notifications', 'IsDeleted') IS NULL
    ALTER TABLE [Notifications] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_Notifications_IsDeleted] DEFAULT CAST(0 AS bit);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropSoftDeleteColumns(migrationBuilder, "Items");
            DropSoftDeleteColumns(migrationBuilder, "Notifications");
        }

        private static void DropSoftDeleteColumns(MigrationBuilder migrationBuilder, string table)
        {
            // Drop the default constraint of IsDeleted first (SQL Server requires it), then the columns.
            migrationBuilder.Sql($@"
DECLARE @constraint nvarchar(200);
SELECT @constraint = d.name
FROM sys.default_constraints d
JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
WHERE d.parent_object_id = OBJECT_ID(N'[{table}]') AND c.name = 'IsDeleted';
IF @constraint IS NOT NULL EXEC('ALTER TABLE [{table}] DROP CONSTRAINT [' + @constraint + ']');

IF COL_LENGTH('{table}', 'IsDeleted') IS NOT NULL ALTER TABLE [{table}] DROP COLUMN [IsDeleted];
IF COL_LENGTH('{table}', 'DeletedAt') IS NOT NULL ALTER TABLE [{table}] DROP COLUMN [DeletedAt];
IF COL_LENGTH('{table}', 'DeletedByUserID') IS NOT NULL ALTER TABLE [{table}] DROP COLUMN [DeletedByUserID];");
        }
    }
}