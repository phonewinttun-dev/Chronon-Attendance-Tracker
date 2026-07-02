using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.Analytics;
using ACST.Shared;

namespace ACST.Domain.Features.Analytics;

public interface IAnalyticsService
{
    Task<Result<OverallAnalyticsDto>> GetOverallAnalyticsAsync(long semesterId);
    Task<Result<ModuleAnalyticsDto>> GetModuleAnalyticsAsync(long moduleId, long semesterId);
    Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(long semesterId);
    Task<Result<DashboardDailyWeeklyDto>> GetDashboardDailyWeeklyAsync(long semesterId);
    Task<Result<List<ModuleAnalyticsDto>>> GetDashboardModulesAsync(long semesterId);
    Task UpdateSemesterDashboardSummaryAsync(long semesterId);
    Task UpdateAllActiveSemesterSummariesAsync();
}
