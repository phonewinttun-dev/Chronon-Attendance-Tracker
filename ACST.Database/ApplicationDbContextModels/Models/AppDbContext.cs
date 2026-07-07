using System;
using System.Collections.Generic;
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

    public virtual DbSet<TblRecurringSchedule> TblRecurringSchedules { get; set; }

    public virtual DbSet<TblSemester> TblSemesters { get; set; }

    public virtual DbSet<TblSession> TblSessions { get; set; }

    public virtual DbSet<TblSemesterDashboardSummary> TblSemesterDashboardSummaries { get; set; }

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

        modelBuilder.Entity<TblSemesterDashboardSummary>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("TblSemesterDashboardSummary_pkey");

            entity.ToTable("TblSemesterDashboardSummary");

            entity.Property(e => e.SemesterId).ValueGeneratedNever().HasColumnName("SemesterId");
            entity.Property(e => e.WarningsJson).HasDefaultValueSql("'[]'::text");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Semester)
                .WithOne()
                .HasForeignKey<TblSemesterDashboardSummary>(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("TblSemesterDashboardSummary_semester_id_fkey");
        });

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
        });

        modelBuilder.Entity<TblSemester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tblsemester_pkey");

            entity.ToTable("TblSemester");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<TblSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tblsession_pkey");

            entity.ToTable("TblSession");

            entity.HasIndex(e => e.MagicLinkToken, "TblSession_MagicLinkToken_key").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.MagicLinkToken).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Status).HasDefaultValueSql("'Not Marked'::text");
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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
