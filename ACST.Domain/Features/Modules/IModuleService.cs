using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.Module;
using ACST.Shared;

namespace ACST.Domain.Features.Modules;

public interface IModuleService
{
    Task<PagedResult<ModuleDto>> GetAllModulesAsync(int? pageNumber = null, int? pageSize = null);
    Task<Result<ModuleDto>> GetModuleByIdAsync(long id);
    Task<Result<ModuleDto>> CreateModuleAsync(CreateModuleRequest request);
    Task<Result<ModuleDto>> UpdateModuleAsync(long id, UpdateModuleRequest request);
    Task<Result> DeleteModuleAsync(long id);
}
