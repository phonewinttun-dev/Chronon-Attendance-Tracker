using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.Features.Analytics;
using ACST.Domain.Features.ClassSessions;
using ACST.Domain.Features.GoogleCalendar;
using ACST.Domain.Features.Holidays;
using ACST.Domain.Features.Modules;
using ACST.Domain.Features.RecurringSchedules;
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
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Feature Services
            builder.Services.AddScoped<IModuleService, ModuleService>();
            builder.Services.AddScoped<ISemesterService, SemesterService>();
            builder.Services.AddScoped<IRecurringScheduleService, RecurringScheduleService>();
            builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
            builder.Services.AddScoped<IHolidayService, HolidayService>();
            builder.Services.AddScoped<IClassSessionService, ClassSessionService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
        }
    }
}
