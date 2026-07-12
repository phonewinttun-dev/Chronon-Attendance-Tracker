using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ACST.Domain.DTOs.ClassSession;
using ACST.Shared;

namespace ACST.Domain.Features.ClassSessions;

public interface IClassSessionService
{
    Task<PagedResult<ClassSessionDto>> GetSessionsAsync(GetClassSessionsRequest request);
    Task<Result<ClassSessionDto>> GetSessionByIdAsync(long id);
    Task<Result> GenerateSessionsAsync(GenerateSessionsRequest request);
    Task<Result> UpdateSessionStatusAsync(long id, UpdateSessionStatusRequest request);
    Task<Result> BulkUpdateSessionStatusAsync(BulkUpdateSessionStatusRequest request);
    Task<Result<string>> MarkAttendanceWithMagicLinkAsync(Guid token);
    Task<Result> UpdateSessionAsync(long id, UpdateClassSessionRequest request);
    Task<Result> DeleteSessionAsync(long id);
    Task SyncGoogleCalendarEventAsync(long sessionId);
}
