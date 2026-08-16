using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace IAMS.Infrastructure.Persistence;

/// <summary>Configuration for bootstrapping the initial administrator account (section "SeedAdmin").</summary>
public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    /// <summary>Whether to create the admin outside Development/Testing (e.g. Production). Default false.</summary>
    public bool Enabled { get; set; }

    public string Username { get; set; } = "admin";
    public string? Password { get; set; }
    public string Email { get; set; } = "admin@iams.local";
    public string FullName { get; set; } = "System Administrator";

    /// <summary>Defaults to true (secure). Set to false for automation/tests that must log in directly.</summary>
    public bool MustChangePassword { get; set; } = true;
}

/// <summary>
/// Idempotent seeder that creates the system roles, default departments
/// and (in development/testing only) a default administrator account.
/// </summary>
public static class ApplicationDbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, bool includeDefaultAdmin, SeedAdminOptions? seedAdmin = null)
    {
        await SeedRolesAndDepartmentsAsync(context);
        if (includeDefaultAdmin || seedAdmin?.Enabled == true)
            await SeedDefaultAdminAsync(context, seedAdmin ?? new SeedAdminOptions(), includeDefaultAdmin);
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

    private static async Task SeedDefaultAdminAsync(
        ApplicationDbContext context,
        SeedAdminOptions options,
        bool allowDefaultPassword)
    {
        var username = string.IsNullOrWhiteSpace(options.Username) ? "admin" : options.Username.Trim();
        if (context.Users.Any(u => u.NormalizedUsername == username.ToUpperInvariant()))
            return;

        var password = options.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            if (allowDefaultPassword)
            {
                password = "Admin@1234";
            }
            else
            {
                throw new InvalidOperationException(
                    "SeedAdmin is enabled but 'SeedAdmin:Password' (env: SeedAdmin__Password) is not set.");
            }
        }

        var adminRole = context.Roles.First(r => r.NormalizedName == RoleConstants.Normalize(RoleConstants.Administrator));
        var hasher = new PasswordHasher<User>();

        var admin = new User
        {
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            Email = string.IsNullOrWhiteSpace(options.Email) ? "admin@iams.local" : options.Email.Trim(),
            NormalizedEmail = (string.IsNullOrWhiteSpace(options.Email) ? "admin@iams.local" : options.Email.Trim()).ToUpperInvariant(),
            FullName = string.IsNullOrWhiteSpace(options.FullName) ? "System Administrator" : options.FullName.Trim(),
            IsActive = true,
            MustChangePassword = options.MustChangePassword
        };

        admin.PasswordHash = hasher.HashPassword(admin, password);

        context.Users.Add(admin);
        context.UserRoles.Add(new UserRole { User = admin, Role = adminRole });
        await context.SaveChangesAsync();
    }
}