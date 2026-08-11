using Hangfire;
using Hangfire.PostgreSql;
using IAMS.Api.Authorization;
using IAMS.Api.Hubs;
using IAMS.Api.Jobs;
using IAMS.Api.Middleware;
using IAMS.Api.Services;
using IAMS.Application;
using IAMS.Application.Common.Interfaces;
using IAMS.Infrastructure;
using IAMS.Infrastructure.Common;
using IAMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, ApiCurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT authentication
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            // SignalR clients pass the JWT via the query string (WebSockets can't set headers).
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<IAMS.Application.Common.Interfaces.INotificationNotifier, SignalRNotificationNotifier>();

builder.Services.AddAuthorization(options => options.Register());

builder.Services.AddSignalR();

// Rate limiting (anti brute-force)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// Health checks
var healthChecks = builder.Services.AddHealthChecks();
if (builder.Environment.EnvironmentName != "Testing")
{
    healthChecks.AddDbContextCheck<ApplicationDbContext>("database");

    // Hangfire: background jobs + dashboard (PostgreSQL storage). Skipped in Testing env.
    var hangfireConnection = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnection)));

    builder.Services.AddHangfireServer();
    builder.Services.AddHostedService<HangfireJobScheduler>();
    builder.Services.AddSingleton<IReportGenerationJob, ReportGenerationJob>();
    builder.Services.AddSingleton<IAuditReportJobQueue, HangfireAuditReportJobQueue>();
}
else
{
    builder.Services.AddSingleton<IAuditReportJobQueue, NoopAuditReportJobQueue>();
}

// CORS origins (default to client dev server when not configured)
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

// Apply migrations and seed data
if (app.Environment.EnvironmentName != "Testing")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await ApplicationDbSeeder.SeedAsync(db);
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseExceptionHandling();
app.UseRateLimiter();

app.UseHttpsRedirection();
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.EnvironmentName != "Testing")
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter()],
        AppPath = "/",
        StatsPollingInterval = 5000
    });
}

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

public partial class Program { }
