using ACST.Api.Middleware;
using ACST.Domain.DTOs.Module;
using ACST.Domain.Features.Modules;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ModulesController : ControllerBase
{
    private readonly IModuleService _moduleService;

    public ModulesController(IModuleService moduleService)
    {
        _moduleService = moduleService;
    }

    [HttpGet]
    [HasPermission(Permissions.Modules.View)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request, long? semesterId = null)
    {
        var result = await _moduleService.GetAllModulesAsync(request, semesterId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    [HasPermission(Permissions.Modules.View)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _moduleService.GetModuleByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Modules.Create)]
    public async Task<IActionResult> Create([FromBody] CreateModuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _moduleService.CreateModuleAsync(request);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [HasPermission(Permissions.Modules.Update)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateModuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _moduleService.UpdateModuleAsync(id, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    [HasPermission(Permissions.Modules.Delete)]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _moduleService.DeleteModuleAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
