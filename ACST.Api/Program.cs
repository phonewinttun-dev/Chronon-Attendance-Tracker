using ACST.Domain.Features;
using ACST.Domain.Features.Analytics;
using ACST.Domain.Features.Notifications;
using Hangfire;
using Hangfire.PostgreSql;
using Scalar.AspNetCore;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.File("logs/chronon_log.txt", rollingInterval: RollingInterval.Hour)
        .CreateLogger();

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddUserSecrets<Program>(optional: true);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();

    // Add Domain feature services and DB Context
    builder.AddDomain();

    // Configure Hangfire Background Jobs
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

    builder.Services.AddHangfireServer();

    // Add CORS policy for Blazor WebApp
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
    });

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseCors("AllowAll");

    app.UseHttpsRedirection();

    app.UseHangfireDashboard();

    using (var scope = app.Services.CreateScope())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobManager.AddOrUpdate<IAnalyticsService>(
            "daily-dashboard-update",
            service => service.UpdateAllActiveSemesterSummariesAsync(),
            Cron.Daily);

        recurringJobManager.AddOrUpdate<INotificationService>(
            "upcoming-class-notifications",
            service => service.ProcessUpcomingClassNotificationsAsync(),
            "*/5 * * * *");

        recurringJobManager.AddOrUpdate<INotificationService>(
            "post-class-30min-reminders",
            service => service.ProcessPostClass30MinRemindersAsync(),
            "*/5 * * * *");

        recurringJobManager.AddOrUpdate<INotificationService>(
            "evening-attendance-reminder",
            service => service.ProcessEveningAttendanceRemindersAsync(),
            "30 18 * * *");

        recurringJobManager.AddOrUpdate<INotificationService>(
            "cleanup-old-notifications",
            service => service.PurgeOldNotificationsAsync(),
            "0 2 * * *");
    }

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
