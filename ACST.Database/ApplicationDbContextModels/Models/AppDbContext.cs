using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ACST.Database.ApplicationDbContextModels.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblHoliday> TblHolidays { get; set; }

    public virtual DbSet<TblModule> TblModules { get; set; }

    public virtual DbSet<TblRecurringSchedule> TblRecurringSchedules { get; set; }

    public virtual DbSet<TblSemester> TblSemesters { get; set; }

    public virtual DbSet<TblSession> TblSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=db.dzeubdrqbzbudbblvxep.supabase.co;Database=postgres;Username=postgres;Password=ACSTSupabase1735#;SSL Mode=Require;Trust Server Certificate=true;Keepalive=30;Timeout=30;Command Timeout=30");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("moddatetime")
            .HasPostgresExtension("vault", "supabase_vault");

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
            entity.HasKey(e => e.Id).HasName("modules_pkey");

            entity.ToTable("TblModule");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.TeacherName).HasColumnName("teacher_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Semester).WithMany(p => p.TblModules)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("TblModule_semester_id_fkey");
        });

        modelBuilder.Entity<TblRecurringSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recurring_schedules_pkey");

            entity.ToTable("TblRecurringSchedule");

            entity.HasIndex(e => new { e.ModuleId, e.SemesterId }, "idx_tblrecurringschedule_module_semester");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DayOfWeek)
                .HasComment("0=Sunday ... 6=Saturday")
                .HasColumnName("day_of_week");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.ModuleId).HasColumnName("module_id");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Module).WithMany(p => p.TblRecurringSchedules)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("recurring_schedules_module_id_fkey");

            entity.HasOne(d => d.Semester).WithMany(p => p.TblRecurringSchedules)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("recurring_schedules_semester_id_fkey");
        });

        modelBuilder.Entity<TblSemester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("semesters_pkey");

            entity.ToTable("TblSemester");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "idx_tblsemester_dates");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<TblSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_sessions_pkey");

            entity.ToTable("TblSession");

            entity.HasIndex(e => e.MagicLinkToken, "class_sessions_magic_link_token_key").IsUnique();

            entity.HasIndex(e => e.SessionDate, "idx_tblsession_date");

            entity.HasIndex(e => e.MagicLinkToken, "idx_tblsession_magic_token");

            entity.HasIndex(e => new { e.ModuleId, e.SessionDate }, "idx_tblsession_module_date");

            entity.HasIndex(e => new { e.SemesterId, e.SessionDate }, "idx_tblsession_semester_date");

            entity.HasIndex(e => e.Status, "idx_tblsession_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDatetime).HasColumnName("end_datetime");
            entity.Property(e => e.GoogleEventId).HasColumnName("google_event_id");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.MagicLinkToken)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("magic_link_token");
            entity.Property(e => e.ModuleId).HasColumnName("module_id");
            entity.Property(e => e.RecurringScheduleId).HasColumnName("recurring_schedule_id");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.SessionDate).HasColumnName("session_date");
            entity.Property(e => e.StartDatetime).HasColumnName("start_datetime");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'Not Marked'::text")
                .HasComment("Not Marked, Present, Absent, Cancelled, Holiday")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Module).WithMany(p => p.TblSessions)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("class_sessions_module_id_fkey");

            entity.HasOne(d => d.RecurringSchedule).WithMany(p => p.TblSessions)
                .HasForeignKey(d => d.RecurringScheduleId)
                .HasConstraintName("class_sessions_recurring_schedule_id_fkey");

            entity.HasOne(d => d.Semester).WithMany(p => p.TblSessions)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("class_sessions_semester_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
