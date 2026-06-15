using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.Holiday;
using ACST.Shared;

namespace ACST.Domain.Features.Holidays;

public interface IHolidayService
{
    Task<Result<IEnumerable<HolidayDto>>> GetAllHolidaysAsync();
    Task<Result<HolidayDto>> CreateHolidayAsync(CreateHolidayRequest request);
    Task<Result> SeedHolidaysAsync();
    Task<Result> DeleteHolidayAsync(long id);
}
