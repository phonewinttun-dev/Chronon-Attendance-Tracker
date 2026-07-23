namespace ACST.Domain.DTOs.ClassSession;

public class DashboardAttendanceRequest
{
    public long SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
}
