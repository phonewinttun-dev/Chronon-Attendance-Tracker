using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.RecurringSchedule;
using ACST.Shared;

namespace ACST.Domain.Features.RecurringSchedules;

public interface IRecurringScheduleService
{
    Task<Result<IEnumerable<RecurringScheduleDto>>> GetSchedulesByModuleAsync(long moduleId);
    Task<Result<RecurringScheduleDto>> CreateScheduleAsync(long moduleId, long semesterId, CreateRecurringScheduleRequest request);
    Task<Result> DeleteScheduleAsync(long id);
}
