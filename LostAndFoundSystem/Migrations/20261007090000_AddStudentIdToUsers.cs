using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LostAndFoundSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Written with IF checks so it is safe even if Program.cs already added the column.
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'StudentID') IS NULL
    ALTER TABLE [Users] ADD [StudentID] nvarchar(20) NULL;");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Users_StudentID'
                 AND object_id = OBJECT_ID('Users'))
    CREATE UNIQUE INDEX [IX_Users_StudentID] ON [Users]([StudentID]) WHERE [StudentID] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_Users_StudentID'
             AND object_id = OBJECT_ID('Users'))
    DROP INDEX [IX_Users_StudentID] ON [Users];");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'StudentID') IS NOT NULL
    ALTER TABLE [Users] DROP COLUMN [StudentID];");
        }
    }
}
