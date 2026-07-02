using ACST.Domain.Features;
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
