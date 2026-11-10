using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AttendanceManagement.Services.Identity.Data;

#nullable disable

namespace AttendanceManagement.Services.Identity.Data.Migrations;

/// <summary>
/// EF Core Migration: Upgrades refresh token storage from raw plaintext to SHA-256 hashes
/// and introduces optimistic concurrency checking via ConcurrencyStamp.
/// </summary>
[DbContext(typeof(IdentityDbContext))]
[Migration("20261107_UpdateRefreshTokenSecurity")]
public partial class UpdateRefreshTokenSecurity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Invalidate legacy records:
        // Delete all legacy refresh tokens from the table so raw plaintext tokens are NEVER
        // mistakenly treated as or compared against SHA-256 hashes.
        // Users with active sessions will simply re-authenticate once.
        migrationBuilder.Sql("DELETE FROM [RefreshTokens];");

        // 2. Drop legacy index on plaintext Token column if it exists
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_Token' AND object_id = OBJECT_ID('RefreshTokens'))
            BEGIN
                DROP INDEX [IX_RefreshTokens_Token] ON [RefreshTokens];
            END
        ");

        // 3. Drop legacy plaintext Token column if it exists
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.columns WHERE name = 'Token' AND object_id = OBJECT_ID('RefreshTokens'))
            BEGIN
                ALTER TABLE [RefreshTokens] DROP COLUMN [Token];
            END
        ");

        // 4. Add new TokenHash column (SHA-256 hash storage)
        migrationBuilder.AddColumn<string>(
            name: "TokenHash",
            table: "RefreshTokens",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: false,
            defaultValue: "");

        // 5. Add ConcurrencyStamp column for optimistic concurrency control
        migrationBuilder.AddColumn<string>(
            name: "ConcurrencyStamp",
            table: "RefreshTokens",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValueSql: "NEWID()");

        // 6. Create unique index on TokenHash
        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "TokenHash",
            table: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "ConcurrencyStamp",
            table: "RefreshTokens");

        migrationBuilder.AddColumn<string>(
            name: "Token",
            table: "RefreshTokens",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens",
            column: "Token",
            unique: true);
    }
}
