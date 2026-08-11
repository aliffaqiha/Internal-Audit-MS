using IAMS.Application.Common.Interfaces;
using IAMS.Application.Notifications;
using IAMS.Infrastructure.Common;
using IAMS.Infrastructure.Emails;
using IAMS.Infrastructure.Persistence;
using IAMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IAMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IDateTimeService, DateTimeService>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<ReminderOptions>(configuration.GetSection(ReminderOptions.SectionName));

        services.AddScoped<ITokenProvider, JwtTokenProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IAuditService, AuditService>();

        // Async email pipeline: enqueue immediately, deliver in the background.
        services.AddSingleton<IEmailQueue, EmailQueue>();
        services.AddScoped<IEmailService, QueuedEmailService>();
        services.AddSingleton<IEmailDispatcher, EmailDispatcher>();
        services.AddHostedService<EmailBackgroundService>();

        // In-app notifications.
        services.AddScoped<INotificationService, NotificationService>();

        // CAP due/overdue reminders (periodic + on demand).
        services.AddSingleton<ICapReminderService, CapReminderService>();
        services.AddHostedService(sp => (Microsoft.Extensions.Hosting.BackgroundService)sp.GetRequiredService<ICapReminderService>());

        services.AddSingleton<IObjectStorageService, ObjectStorageService>();

        return services;
    }
}