using System.Threading.Tasks;
using ACST.Domain.DTOs.Module;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Domain.Features.Modules;

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
    public async Task<IActionResult> GetAll([FromQuery] int? pageNumber, [FromQuery] int? pageSize, [FromQuery] long? semesterId)
    {
        var result = await _moduleService.GetAllModulesAsync(pageNumber, pageSize, semesterId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _moduleService.GetModuleByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _moduleService.CreateModuleAsync(request);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateModuleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _moduleService.UpdateModuleAsync(id, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _moduleService.DeleteModuleAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
