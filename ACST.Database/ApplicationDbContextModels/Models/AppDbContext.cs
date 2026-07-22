using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.EntityFrameworkCore;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblHoliday> TblHolidays { get; set; }

    public virtual DbSet<TblModule> TblModules { get; set; }

    public virtual DbSet<TblPermission> TblPermissions { get; set; }

    public virtual DbSet<TblRecurringSchedule> TblRecurringSchedules { get; set; }

    public virtual DbSet<TblRole> TblRoles { get; set; }

    public virtual DbSet<TblRolepermission> TblRolepermissions { get; set; }

    public virtual DbSet<TblSemester> TblSemesters { get; set; }

    public virtual DbSet<TblSemesterDashboardSummary> TblSemesterDashboardSummaries { get; set; }

    public virtual DbSet<TblSession> TblSessions { get; set; }

    public virtual DbSet<TblUser> TblUsers { get; set; }

    public virtual DbSet<TblUsertoken> TblUsertokens { get; set; }

    public virtual DbSet<TblNotification> TblNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("extensions", "uuid-ossp");

        modelBuilder.Entity<TblHoliday>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("holidays_pkey");

            entity.ToTable("TblHoliday");

            entity.HasIndex(e => e.HolidayDate, "holidays_holiday_date_key").IsUnique();

            entity.HasIndex(e => e.HolidayDate, "idx_tblholiday_date");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.HolidayDate).HasColumnName("holiday_date");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<TblModule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tblmodule_pkey");

            entity.ToTable("TblModule");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Semester).WithMany(p => p.TblModules)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("tblmodule_semester_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TblModules)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TblModule_User");
        });

        modelBuilder.Entity<TblPermission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("tblpermission_pkey");

            entity.ToTable("TblPermission");

            entity.Property(e => e.PermissionId).UseIdentityAlwaysColumn();
            entity.Property(e => e.DeleteFlag).HasDefaultValue(false);
        });

        modelBuilder.Entity<TblRecurringSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tblrecurringschedule_pkey");

            entity.ToTable("TblRecurringSchedule");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Module).WithMany(p => p.TblRecurringSchedules)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("recurring_schedules_module_id_fkey");

            entity.HasOne(d => d.Semester).WithMany(p => p.TblRecurringSchedules)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("recurring_schedules_semester_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TblRecurringSchedules)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TblRecurringSchedule_User");
        });

        modelBuilder.Entity<TblRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("tblrole_pkey");

            entity.ToTable("TblRole");

            entity.Property(e => e.RoleId).UseIdentityAlwaysColumn();
            entity.Property(e => e.DeleteFlag).HasDefaultValue(false);
        });

        modelBuilder.Entity<TblRolepermission>(entity =>
        {
            entity.HasKey(e => e.RolePermissionId).HasName("tblrolepermission_pkey");

            entity.ToTable("TblRolePermission");

            entity.Property(e => e.RolePermissionId).UseIdentityAlwaysColumn();
            entity.Property(e => e.DeleteFlag).HasDefaultValue(false);

            entity.HasOne(d => d.Permission).WithMany(p => p.TblRolepermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("tblrolepermission_permission_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.TblRolepermissions)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("tblrolepermission_role_id_fkey");
        });

        modelBuilder.Entity<TblSemester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tblsemester_pkey");

            entity.ToTable("TblSemester");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.TblSemesters)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TblSemester_User");
        });

        modelBuilder.Entity<TblSemesterDashboardSummary>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("TblSemesterDashboardSummary_pkey");

            entity.ToTable("TblSemesterDashboardSummary");

            entity.Property(e => e.SemesterId).ValueGeneratedNever();
            entity.Property(e => e.WarningsJson).HasDefaultValueSql("'[]'::text");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Semester)
                .WithOne()
                .HasForeignKey<TblSemesterDashboardSummary>(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("TblSemesterDashboardSummary_semester_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TblSemesterDashboardSummaries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TblSemesterDashboardSummary_User");
        });

        modelBuilder.Entity<TblSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tblsession_pkey");

            entity.ToTable("TblSession");

            entity.HasIndex(e => e.MagicLinkToken, "TblSession_MagicLinkToken_key").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.Status).HasDefaultValueSql("'Not Marked'::text");
            entity.Property(e => e.MagicLinkToken).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Module).WithMany(p => p.TblSessions)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("class_sessions_module_id_fkey");

            entity.HasOne(d => d.RecurringSchedule).WithMany(p => p.TblSessions)
                .HasForeignKey(d => d.RecurringScheduleId)
                .HasConstraintName("class_sessions_recurring_schedule_id_fkey");

            entity.HasOne(d => d.Semester).WithMany(p => p.TblSessions)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("class_sessions_semester_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TblSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TblSession_User");
        });

        modelBuilder.Entity<TblUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("tbluser_pkey");

            entity.ToTable("TblUser");

            entity.Property(e => e.UserId).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.DeleteFlag).HasDefaultValue(false);

            entity.HasOne(d => d.Role).WithMany(p => p.TblUsers)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("tbluser_role_id_fkey");
        });

        modelBuilder.Entity<TblUsertoken>(entity =>
        {
            entity.HasKey(e => e.UserTokenId).HasName("tblusertoken_pkey");

            entity.ToTable("TblUserToken");

            entity.Property(e => e.UserTokenId).UseIdentityAlwaysColumn();
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.DeleteFlag).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.TblUsertokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("tblusertoken_user_id_fkey");
        });

        modelBuilder.Entity<TblNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("tblnotification_pkey");

            entity.ToTable("TblNotification");

            entity.Property(e => e.NotificationId).UseIdentityAlwaysColumn();
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.TriggeredAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.TblNotifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TblNotification_User");

            entity.HasOne(d => d.Session).WithMany(p => p.TblNotifications)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TblNotification_Session");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
