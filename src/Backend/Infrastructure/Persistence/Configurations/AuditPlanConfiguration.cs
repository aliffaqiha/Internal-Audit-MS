using IAMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IAMS.Infrastructure.Persistence.Configurations;

public sealed class AuditPlanConfiguration : IEntityTypeConfiguration<AuditPlan>
{
    public void Configure(EntityTypeBuilder<AuditPlan> builder)
    {
        builder.ToTable("AuditPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Objective).HasMaxLength(1000);
        builder.Property(x => x.Scope).HasMaxLength(1000);
        builder.Property(x => x.Standard).HasMaxLength(100);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class AuditAssignmentConfiguration : IEntityTypeConfiguration<AuditAssignment>
{
    public void Configure(EntityTypeBuilder<AuditAssignment> builder)
    {
        builder.ToTable("AuditAssignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoleInPlan).HasMaxLength(60);

        builder.HasIndex(x => new { x.AuditPlanId, x.UserId }).IsUnique();

        builder.HasOne(x => x.AuditPlan)
            .WithMany(p => p.Assignments)
            .HasForeignKey(x => x.AuditPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AuditChecklistItemConfiguration : IEntityTypeConfiguration<AuditChecklistItem>
{
    public void Configure(EntityTypeBuilder<AuditChecklistItem> builder)
    {
        builder.ToTable("AuditChecklistItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Question).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasOne(x => x.AuditPlan)
            .WithMany(p => p.ChecklistItems)
            .HasForeignKey(x => x.AuditPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}