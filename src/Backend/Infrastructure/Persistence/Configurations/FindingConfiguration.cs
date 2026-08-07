using IAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IAMS.Infrastructure.Persistence.Configurations;

public sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("Findings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Recommendation).HasMaxLength(2000);
        builder.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.RiskLevel);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.DepartmentId);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AuditPlan)
            .WithMany()
            .HasForeignKey(x => x.AuditPlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FindingEvidenceConfiguration : IEntityTypeConfiguration<FindingEvidence>
{
    public void Configure(EntityTypeBuilder<FindingEvidence> builder)
    {
        builder.ToTable("FindingEvidences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredObjectName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();

        builder.HasIndex(x => new { x.FindingId, x.Version });

        builder.HasOne(x => x.Finding)
            .WithMany(f => f.Evidences)
            .HasForeignKey(x => x.FindingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}