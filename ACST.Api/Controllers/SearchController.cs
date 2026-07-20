using ACST.Domain.DTOs.Search;
using ACST.Domain.Features.Search;
using ACST.Shared;
using Microsoft.AspNetCore.Mvc;

namespace ACST.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("modules")]
    public async Task<IActionResult> SearchModules([FromQuery] SearchDto searchRequest, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] long? semesterId = null)
    {
        var result = await _searchService.SearchModuleAsync(searchRequest, pageNumber, pageSize, semesterId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("semesters")]
    public async Task<IActionResult> SearchSemesters([FromQuery] SearchDto searchRequest, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _searchService.SearchSemesterAsync(searchRequest, pageNumber, pageSize);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> SearchSessions([FromQuery] SearchDto searchRequest, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] long? semesterId = null, [FromQuery] long? moduleId = null)
    {
        var result = await _searchService.SearchSessionAsync(searchRequest, pageNumber, pageSize, semesterId, moduleId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
