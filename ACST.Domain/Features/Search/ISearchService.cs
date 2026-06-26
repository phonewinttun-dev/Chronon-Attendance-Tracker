using ACST.Domain.DTOs.Search;
using ACST.Shared;

namespace ACST.Domain.Features.Search
{
    public interface ISearchService
    {
        Task<PagedResult<SearchModuleDto>> SearchModuleAsync(SearchDto searchRequest, int pageNumber = 1, int pageSize = 10, long? semesterId = null);
        Task<PagedResult<SearchSemesterDto>> SearchSemesterAsync(SearchDto searchRequest, int pageNumber = 1, int pageSize = 10);
        Task<PagedResult<SearchClassSessionDto>> SearchSessionAsync(SearchDto searchRequest, int pageNumber = 1, int pageSize = 10, long? semesterId = null, long? moduleId = null);
    }
}

