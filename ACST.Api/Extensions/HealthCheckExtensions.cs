using ACST.Database.ApplicationDbContextModels.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace ACST.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddDatabaseHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(
                name: "supabase_db",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "supabase", "postgres" });

        return services;
    }

    public static IEndpointConventionBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var response = new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.ToString(),
                    entries = report.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => new
                        {
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description ?? (entry.Value.Status == HealthStatus.Healthy 
                                ? "Database is healthy and responsive." 
                                : "Database check failed."),
                            duration = entry.Value.Duration.ToString(),
                            tags = entry.Value.Tags,
                            error = entry.Value.Exception?.Message
                        })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
        });
    }
}
