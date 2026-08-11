using IAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IAMS.Infrastructure.Persistence.Configurations;

public sealed class AuditReportConfiguration : IEntityTypeConfiguration<AuditReport>
{
    public void Configure(EntityTypeBuilder<AuditReport> builder)
    {
        builder.ToTable("AuditReports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ObjectName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();

        builder.HasIndex(x => x.AuditPlanId).IsUnique();

        builder.HasOne(x => x.AuditPlan)
            .WithMany()
            .HasForeignKey(x => x.AuditPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
