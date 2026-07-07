using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.Holiday;
using ACST.Shared;

namespace ACST.Domain.Features.Holidays;

public interface IHolidayService
{
    Task<PagedResult<HolidayDto>> GetAllHolidaysAsync(int? pageNumber = null, int? pageSize = null);
    Task<Result<HolidayDto>> CreateHolidayAsync(CreateHolidayRequest request);
    Task<Result> DeleteHolidayAsync(long id);
    Task<Result<int>> ImportGoogleCalendarHolidaysAsync(string calendarId, DateOnly startDate, DateOnly endDate);
}
