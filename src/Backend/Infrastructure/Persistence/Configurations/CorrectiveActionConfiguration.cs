using IAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IAMS.Infrastructure.Persistence.Configurations;

public sealed class CorrectiveActionConfiguration : IEntityTypeConfiguration<CorrectiveAction>
{
    public void Configure(EntityTypeBuilder<CorrectiveAction> builder)
    {
        builder.ToTable("CorrectiveActions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.PicName).HasMaxLength(150);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.VerificationNote).HasMaxLength(1000);
        builder.Property(x => x.AttachmentFileName).HasMaxLength(255);
        builder.Property(x => x.AttachmentObjectName).HasMaxLength(500);
        builder.Property(x => x.AttachmentContentType).HasMaxLength(120);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(x => new { x.FindingId }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);

        // Full-text search over Action/PicName/RejectionReason/VerificationNote (GIN expression index).
        builder.HasIndex(x => new { x.Action, x.PicName, x.RejectionReason, x.VerificationNote })
            .HasMethod("GIN")
            .IsTsVectorExpressionIndex("simple");

        builder.HasOne(x => x.Finding)
            .WithMany()
            .HasForeignKey(x => x.FindingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}