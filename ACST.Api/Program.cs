using ACST.Domain.Features;
using ACST.Domain.Features.Analytics;
using ACST.Domain.Features.Notifications;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

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
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
        {
            UseNativeDatabaseTransactions = true,
            DistributedLockTimeout = TimeSpan.FromSeconds(30)
        }));

    builder.Services.AddHangfireServer();

    // Configure JWT Authentication & Authorization
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (!string.IsNullOrEmpty(secretKey))
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };
        });
    }

    builder.Services.AddAuthorization();

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
    app.UseCors("AllowAll");

    app.UseHttpsRedirection();

    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseAuthentication();
    app.UseAuthorization();

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
