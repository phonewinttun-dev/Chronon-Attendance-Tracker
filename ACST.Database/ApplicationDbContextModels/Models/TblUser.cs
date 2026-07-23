namespace ACST.Database.ApplicationDbContextModels.Models
{
    public partial class TblUser
    {
        public int UserId { get; set; }

        public int? RoleId { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? MobileNum { get; set; }

        public string? PasswordHash { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? DeleteFlag { get; set; }

        public virtual TblRole? Role { get; set; }

        public virtual ICollection<TblUsertoken> TblUsertokens { get; set; } = new List<TblUsertoken>();

        public virtual ICollection<TblSemester> TblSemesters { get; set; } = new List<TblSemester>();

        public virtual ICollection<TblModule> TblModules { get; set; } = new List<TblModule>();

        public virtual ICollection<TblRecurringSchedule> TblRecurringSchedules { get; set; } = new List<TblRecurringSchedule>();

        public virtual ICollection<TblSession> TblSessions { get; set; } = new List<TblSession>();

        public virtual ICollection<TblSemesterDashboardSummary> TblSemesterDashboardSummaries { get; set; } = new List<TblSemesterDashboardSummary>();

        public virtual ICollection<TblNotification> TblNotifications { get; set; } = new List<TblNotification>();
    }
}
