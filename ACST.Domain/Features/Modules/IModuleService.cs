using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.Module;
using ACST.Shared;

namespace ACST.Domain.Features.Modules;

public interface IModuleService
{
    Task<Result<IEnumerable<ModuleDto>>> GetAllModulesAsync();
    Task<Result<ModuleDto>> GetModuleByIdAsync(long id);
    Task<Result<ModuleDto>> CreateModuleAsync(CreateModuleRequest request);
    Task<Result<ModuleDto>> UpdateModuleAsync(long id, UpdateModuleRequest request);
    Task<Result> DeleteModuleAsync(long id);
}
