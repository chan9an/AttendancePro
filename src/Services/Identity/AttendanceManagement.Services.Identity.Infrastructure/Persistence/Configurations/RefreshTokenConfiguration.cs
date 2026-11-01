using AttendanceManagement.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AttendanceManagement.Services.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the RefreshToken entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

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

        // Unique index on Token string for instant lookup and uniqueness guarantees
        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        // Index on UserId to support querying all tokens belonging to a user (e.g., revoking all sessions upon password reset)
        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");
    }
}
