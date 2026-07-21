using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.Features.Analytics;
using ACST.Domain.Features.Auth;
using ACST.Domain.Features.ClassSessions;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Holidays;
using ACST.Domain.Features.Modules;
using ACST.Domain.Features.Notifications;
using ACST.Domain.Features.RecurringSchedules;
using ACST.Domain.Features.RolePermission;
using ACST.Domain.Features.Search;
using ACST.Domain.Features.Semesters;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ACST.Domain.Features
{
    public static class FeatureManager
    {
        public static void AddDomain(this WebApplicationBuilder builder)
        {
            // Database
            builder.Services.AddDbContextPool<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Feature Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
            builder.Services.AddScoped<IModuleService, ModuleService>();
            builder.Services.AddScoped<ISemesterService, SemesterService>();
            builder.Services.AddScoped<IRecurringScheduleService, RecurringScheduleService>();
            var googleConfig = builder.Configuration.GetSection("GoogleCalendar");
            var enabled = googleConfig.GetValue<bool>("Enabled", false);
            var hasCredentials = !string.IsNullOrEmpty(googleConfig["ClientId"]) && !string.IsNullOrEmpty(googleConfig["ClientSecret"]);

            if (enabled && hasCredentials)
            {
                builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
            }
            else
            {
                builder.Services.AddScoped<IGoogleCalendarService, DisabledGoogleCalendarService>();
            }
            builder.Services.AddScoped<IHolidayService, HolidayService>();
            builder.Services.AddScoped<IClassSessionService, ClassSessionService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
        }
    }
}
