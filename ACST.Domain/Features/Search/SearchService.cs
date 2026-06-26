using ACST.Database.ApplicationDbContextModels.Models;
using ACST.Domain.DTOs.Search;
using ACST.Shared;
using Microsoft.EntityFrameworkCore;

namespace ACST.Domain.Features.Search
{
    public class SearchService : ISearchService
    {
        private readonly AppDbContext _context;

        public SearchService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<SearchModuleDto>> SearchModuleAsync(SearchDto searchRequest, int pageNumber = 1, int pageSize = 10, long? semesterId = null)
        {
            try
            {
                var query = _context.TblModules
                    .AsNoTracking()
                    .Include(m => m.Semester)
                    .Where(m => !m.IsDeleted);

                if (semesterId.HasValue)
                {
                    query = query.Where(m => m.SemesterId == semesterId.Value);
                }

                if (searchRequest != null)
                {
                    if (!string.IsNullOrWhiteSpace(searchRequest.Name))
                    {
                        var term = searchRequest.Name;
                        query = query.Where(m => EF.Functions.ToTsVector("english", m.Name + " " + (m.TeacherName ?? "") + " " + m.ModuleCode)
                            .Matches(term));
                    }
                    if (!string.IsNullOrWhiteSpace(searchRequest.ModuleCode))
                    {
                        query = query.Where(m => m.ModuleCode.Contains(searchRequest.ModuleCode));
                    }
                    if (!string.IsNullOrWhiteSpace(searchRequest.TeacherName))
                    {
                        query = query.Where(m => m.TeacherName != null && EF.Functions.ToTsVector("english", m.TeacherName)
                            .Matches(searchRequest.TeacherName));
                    }
                }

                query = query.OrderBy(m => m.Name);

                int totalCount = await query.CountAsync();

                var rawItems = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new SearchModuleDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        ModuleCode = m.ModuleCode,
                        TeacherName = m.TeacherName,
                        SemesterId = m.SemesterId,
                        SemesterName = m.Semester != null ? m.Semester.Name : null,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .ToListAsync();

                var pagination = new Pagination(pageNumber, pageSize, totalCount);
                return PagedResult<SearchModuleDto>.Success(rawItems, pagination);
            }
            catch (Exception ex)
            {
                return PagedResult<SearchModuleDto>.Failure($"Failed to search modules: {ex.Message}");
            }
        }

        public async Task<PagedResult<SearchSemesterDto>> SearchSemesterAsync(SearchDto searchRequest, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.TblSemesters
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted);

                if (searchRequest != null && !string.IsNullOrWhiteSpace(searchRequest.Name))
                {
                    var term = searchRequest.Name;
                    query = query.Where(s => EF.Functions.ToTsVector("english", s.Name)
                        .Matches(term));
                }

                query = query.OrderByDescending(s => s.StartDate);

                int totalCount = await query.CountAsync();

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new SearchSemesterDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                var pagination = new Pagination(pageNumber, pageSize, totalCount);
                return PagedResult<SearchSemesterDto>.Success(items, pagination);
            }
            catch (Exception ex)
            {
                return PagedResult<SearchSemesterDto>.Failure($"Failed to search semesters: {ex.Message}");
            }
        }

        public async Task<PagedResult<SearchClassSessionDto>> SearchSessionAsync(SearchDto searchRequest, int pageNumber = 1, int pageSize = 10, long? semesterId = null, long? moduleId = null)
        {
            try
            {
                var query = _context.TblSessions
                    .Include(s => s.Module)
                    .Include(s => s.Semester)
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted && (s.Module == null || !s.Module.IsDeleted) && (s.Semester == null || !s.Semester.IsDeleted));

                if (semesterId.HasValue) query = query.Where(s => s.SemesterId == semesterId.Value);
                if (moduleId.HasValue) query = query.Where(s => s.ModuleId == moduleId.Value);

                if (searchRequest != null)
                {
                    if (!string.IsNullOrWhiteSpace(searchRequest.Name))
                    {
                        var term = searchRequest.Name;
                        query = query.Where(s => s.Module.Name.Contains(term) || (s.Module.TeacherName != null && s.Module.TeacherName.Contains(term)));
                    }
                }

                int totalCount = await query.CountAsync();

                var items = await query
                    .OrderBy(s => s.StartDatetime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new SearchClassSessionDto
                    {
                        Id = s.Id,
                        ModuleId = s.ModuleId,
                        ModuleName = s.Module.Name,
                        ModuleCode = s.Module.ModuleCode,
                        SemesterId = s.SemesterId,
                        SemesterName = s.Semester.Name,
                        SessionDate = s.SessionDate,
                        StartDatetime = s.StartDatetime,
                        EndDatetime = s.EndDatetime,
                        Status = s.Status,
                        MagicLinkToken = s.MagicLinkToken,
                        GoogleEventId = s.GoogleEventId
                    })
                    .ToListAsync();

                var pagination = new Pagination(pageNumber, pageSize, totalCount);
                return PagedResult<SearchClassSessionDto>.Success(items, pagination);
            }
            catch (Exception ex)
            {
                return PagedResult<SearchClassSessionDto>.Failure($"Failed to search class sessions: {ex.Message}");
            }
        }
    }
}



