using System.Reflection;
using FluentValidation;
using IAMS.Application.Common.Behaviors;
using IAMS.Application.Common.Interfaces;
using IAMS.Application.Notifications;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace IAMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}