using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace IAMS.Infrastructure.Persistence;

/// <summary>
/// Idempotent seeder that creates the system roles, default departments
/// and a default administrator account on first startup.
/// </summary>
public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAndDepartmentsAsync(context);
        await SeedDefaultAdminAsync(context);
    }

    private static async Task SeedRolesAndDepartmentsAsync(ApplicationDbContext context)
    {
        var roles = new[]
        {
            (RoleConstants.Administrator, "System administrator with full control"),
            (RoleConstants.Manager, "Audit manager - plans and approves audits"),
            (RoleConstants.Auditor, "Internal auditor - performs audits"),
            (RoleConstants.Auditee, "Department being audited - submits CAP"),
            (RoleConstants.TopManagement, "Top management - views dashboards")
        };

        foreach (var (name, description) in roles)
        {
            if (!context.Roles.Any(r => r.NormalizedName == RoleConstants.Normalize(name)))
            {
                context.Roles.Add(new Role
                {
                    Name = name,
                    NormalizedName = RoleConstants.Normalize(name),
                    Description = description
                });
            }
        }

        var departments = new[] { "Finance", "HR", "IT", "Procurement", "Warehouse", "Production" };
        foreach (var name in departments)
        {
            if (!context.Departments.Any(d => d.Name == name))
                context.Departments.Add(new Department { Name = name, IsActive = true });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultAdminAsync(ApplicationDbContext context)
    {
        const string adminUsername = "admin";
        if (context.Users.Any(u => u.NormalizedUsername == adminUsername.ToUpperInvariant()))
            return;

        var adminRole = context.Roles.First(r => r.NormalizedName == RoleConstants.Normalize(RoleConstants.Administrator));
        var hasher = new PasswordHasher<User>();

        var admin = new User
        {
            Username = adminUsername,
            NormalizedUsername = adminUsername.ToUpperInvariant(),
            Email = "admin@iams.local",
            NormalizedEmail = "ADMIN@IAMS.LOCAL",
            FullName = "System Administrator",
            IsActive = true,
            MustChangePassword = true
        };

        admin.PasswordHash = hasher.HashPassword(admin, "Admin@1234");

        context.Users.Add(admin);
        context.UserRoles.Add(new UserRole { User = admin, Role = adminRole });
        await context.SaveChangesAsync();
    }
}