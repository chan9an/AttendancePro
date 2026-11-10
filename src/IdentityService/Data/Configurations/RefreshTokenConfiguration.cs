using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Data.Configurations;

/// <summary>
/// Fluent API configuration for the RefreshToken entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        // Store only the SHA-256 hash (never raw tokens)
        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(rt => rt.JwtId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(rt => rt.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(rt => rt.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(rt => rt.ExpiresAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        // Optimistic concurrency token
        builder.Property(rt => rt.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(64)
            .IsConcurrencyToken();

        // Unique index on TokenHash for fast indexed lookup and uniqueness guarantee
        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        // Index on UserId for bulk revocation during replay attacks or logout
        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");
    }
}
