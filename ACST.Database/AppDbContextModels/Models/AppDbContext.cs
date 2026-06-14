using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ACST.Database.AppDbContextModels.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ClassSession> ClassSessions { get; set; }

    public virtual DbSet<Holiday> Holidays { get; set; }

    public virtual DbSet<Module> Modules { get; set; }

    public virtual DbSet<RecurringSchedule> RecurringSchedules { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

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

        modelBuilder.Entity<ClassSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_sessions_pkey");

            entity.ToTable("class_sessions");

            entity.HasIndex(e => e.MagicLinkToken, "class_sessions_magic_link_token_key").IsUnique();

            entity.HasIndex(e => e.SessionDate, "idx_class_sessions_date");

            entity.HasIndex(e => e.MagicLinkToken, "idx_class_sessions_magic_token");

            entity.HasIndex(e => new { e.ModuleId, e.SessionDate }, "idx_class_sessions_module_date");

            entity.HasIndex(e => new { e.SemesterId, e.SessionDate }, "idx_class_sessions_semester_date");

            entity.HasIndex(e => e.Status, "idx_class_sessions_status");

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

            entity.HasOne(d => d.Module).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("class_sessions_module_id_fkey");

            entity.HasOne(d => d.RecurringSchedule).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.RecurringScheduleId)
                .HasConstraintName("class_sessions_recurring_schedule_id_fkey");

            entity.HasOne(d => d.Semester).WithMany(p => p.ClassSessions)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("class_sessions_semester_id_fkey");
        });

        modelBuilder.Entity<Holiday>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("holidays_pkey");

            entity.ToTable("holidays");

            entity.HasIndex(e => e.HolidayDate, "holidays_holiday_date_key").IsUnique();

            entity.HasIndex(e => e.HolidayDate, "idx_holidays_date");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.HolidayDate).HasColumnName("holiday_date");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("modules_pkey");

            entity.ToTable("modules");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.TeacherName).HasColumnName("teacher_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<RecurringSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recurring_schedules_pkey");

            entity.ToTable("recurring_schedules");

            entity.HasIndex(e => new { e.ModuleId, e.SemesterId }, "idx_recurring_schedules_module_semester");

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

            entity.HasOne(d => d.Module).WithMany(p => p.RecurringSchedules)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("recurring_schedules_module_id_fkey");

            entity.HasOne(d => d.Semester).WithMany(p => p.RecurringSchedules)
                .HasForeignKey(d => d.SemesterId)
                .HasConstraintName("recurring_schedules_semester_id_fkey");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("semesters_pkey");

            entity.ToTable("semesters");

            entity.HasIndex(e => new { e.StartDate, e.EndDate }, "idx_semesters_dates");

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
