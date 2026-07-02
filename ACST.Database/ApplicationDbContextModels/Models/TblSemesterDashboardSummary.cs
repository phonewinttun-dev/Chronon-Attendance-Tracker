using System;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class TblSemesterDashboardSummary
{
    public long SemesterId { get; set; }
    public string SemesterName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public double SemesterHealthRate { get; set; }
    public int TodaySessionsCount { get; set; }
    public int UpcomingSessionsCount { get; set; }
    public int TotalSessions { get; set; }
    public int PresentSessions { get; set; }
    public int AbsentSessions { get; set; }
    public int LateSessions { get; set; }
    public int CancelledSessions { get; set; }
    public int HolidaySessions { get; set; }
    public int ValidSessions { get; set; }
    public double CalculatedRate { get; set; }
    public double? TodayAttendanceRate { get; set; }
    public string WarningsJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; }

    public virtual TblSemester Semester { get; set; } = null!;
}
