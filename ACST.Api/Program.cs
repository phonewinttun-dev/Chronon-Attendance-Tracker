using ACST.Domain.Features;
using ACST.Domain.Features.Analytics;
using ACST.Domain.Features.Notifications;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.File("logs/chronon_log.txt", rollingInterval: RollingInterval.Hour)
        .CreateLogger();

try
{
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

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });
    });


    // Add JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? "default_secret_key_at_least_32_chars_long";

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

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


    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseCors("AllowAll");

    app.UseHttpsRedirection();

    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = ["Bearer"]
        };
    });

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
