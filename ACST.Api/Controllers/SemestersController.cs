using ACST.Domain.DTOs.Semester;
using ACST.Domain.Features.Semesters;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SemestersController : ControllerBase
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
    {
        _semesterService = semesterService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
    {
        var result = await _semesterService.GetAllSemestersAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _semesterService.GetSemesterByIdAsync(id);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSemesterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _semesterService.CreateSemesterAsync(request);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result) : BadRequest(result);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSemesterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Failure("Invalid request parameters."));

        var result = await _semesterService.UpdateSemesterAsync(id, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _semesterService.DeleteSemesterAsync(id);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
