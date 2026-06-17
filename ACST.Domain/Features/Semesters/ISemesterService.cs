using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.Semester;
using ACST.Shared;

namespace ACST.Domain.Features.Semesters;

public interface ISemesterService
{
    Task<PagedResult<SemesterDto>> GetAllSemestersAsync(string? searchTerm = null, int? pageNumber = null, int? pageSize = null);
    Task<Result<SemesterDto>> GetSemesterByIdAsync(long id);
    Task<Result<SemesterDto>> CreateSemesterAsync(CreateSemesterRequest request);
    Task<Result<SemesterDto>> UpdateSemesterAsync(long id, UpdateSemesterRequest request);
    Task<Result> DeleteSemesterAsync(long id);
}
