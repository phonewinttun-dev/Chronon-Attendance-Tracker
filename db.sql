CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public."TblRole" (
  "RoleID" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RoleName" text,
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblrole_pkey PRIMARY KEY ("RoleID")
);

CREATE TABLE IF NOT EXISTS public."TblPermission" (
  "PermissionID" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "PermissionName" text,
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblpermission_pkey PRIMARY KEY ("PermissionID")
);

CREATE TABLE IF NOT EXISTS public."TblRolePermission" (
  "RolePermissionID" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RoleID" integer,
  "PermissionID" integer,
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblrolepermission_pkey PRIMARY KEY ("RolePermissionID"),
  CONSTRAINT tblrolepermission_role_id_fkey FOREIGN KEY ("RoleID") REFERENCES public."TblRole"("RoleID"),
  CONSTRAINT tblrolepermission_permission_id_fkey FOREIGN KEY ("PermissionID") REFERENCES public."TblPermission"("PermissionID")
);

CREATE TABLE IF NOT EXISTS public."TblUser" (
  "UserID" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RoleID" integer,
  "FullName" text,
  "Email" text,
  "MobileNum" text,
  "PasswordHash" text,
  "CreatedAt" timestamp with time zone DEFAULT now(),
  "UpdatedAt" timestamp with time zone DEFAULT now(),
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tbluser_pkey PRIMARY KEY ("UserID"),
  CONSTRAINT tbluser_role_id_fkey FOREIGN KEY ("RoleID") REFERENCES public."TblRole"("RoleID")
);

CREATE TABLE IF NOT EXISTS public."TblUserToken" (
  "UserTokenID" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "UserID" integer,
  "RefreshToken" text NOT NULL,
  "IsRevoked" boolean DEFAULT false,
  "ExpiresAt" timestamp with time zone,
  "CreatedAt" timestamp with time zone DEFAULT now(),
  "UpdatedAt" timestamp with time zone DEFAULT now(),
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblusertoken_pkey PRIMARY KEY ("UserTokenID"),
  CONSTRAINT tblusertoken_user_id_fkey FOREIGN KEY ("UserID") REFERENCES public."TblUser"("UserID")
);

CREATE TABLE IF NOT EXISTS public."TblHoliday" (
  "id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "holiday_date" date NOT NULL,
  "name" text NOT NULL,
  "is_deleted" boolean NOT NULL DEFAULT false,
  "created_at" timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT holidays_pkey PRIMARY KEY ("id"),
  CONSTRAINT holidays_holiday_date_key UNIQUE ("holiday_date")
);

CREATE INDEX IF NOT EXISTS idx_tblholiday_date ON public."TblHoliday" ("holiday_date");

CREATE TABLE IF NOT EXISTS public."TblSemester" (
  "ID" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "Name" text NOT NULL,
  "StartDate" date NOT NULL,
  "EndDate" date NOT NULL,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UserID" integer,
  CONSTRAINT tblsemester_pkey PRIMARY KEY ("ID"),
  CONSTRAINT "FK_TblSemester_User" FOREIGN KEY ("UserID") REFERENCES public."TblUser"("UserID") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblModule" (
  "ID" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "Name" text NOT NULL,
  "ModuleCode" text NOT NULL,
  "TeacherName" text,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "SemesterID" bigint,
  "UserID" integer,
  CONSTRAINT tblmodule_pkey PRIMARY KEY ("ID"),
  CONSTRAINT tblmodule_semester_id_fkey FOREIGN KEY ("SemesterID") REFERENCES public."TblSemester"("ID"),
  CONSTRAINT "FK_TblModule_User" FOREIGN KEY ("UserID") REFERENCES public."TblUser"("UserID") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblRecurringSchedule" (
  "ID" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "ModuleID" bigint NOT NULL,
  "SemesterID" bigint NOT NULL,
  "DayOfWeek" smallint NOT NULL CHECK ("DayOfWeek" >= 0 AND "DayOfWeek" <= 6),
  "StartTime" time without time zone NOT NULL,
  "EndTime" time without time zone NOT NULL,
  "IsActive" boolean NOT NULL DEFAULT true,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UserID" integer,
  CONSTRAINT tblrecurringschedule_pkey PRIMARY KEY ("ID"),
  CONSTRAINT recurring_schedules_module_id_fkey FOREIGN KEY ("ModuleID") REFERENCES public."TblModule"("ID"),
  CONSTRAINT recurring_schedules_semester_id_fkey FOREIGN KEY ("SemesterID") REFERENCES public."TblSemester"("ID"),
  CONSTRAINT "FK_TblRecurringSchedule_User" FOREIGN KEY ("UserID") REFERENCES public."TblUser"("UserID") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblSession" (
  "ID" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RecurringScheduleID" bigint NOT NULL,
  "ModuleID" bigint NOT NULL,
  "SemesterID" bigint NOT NULL,
  "SessionDate" date NOT NULL,
  "StartDatetime" timestamp with time zone NOT NULL,
  "EndDatetime" timestamp with time zone NOT NULL,
  "Status" text NOT NULL DEFAULT 'Not Marked'::text CHECK ("Status" = ANY (ARRAY['Not Marked'::text, 'Present'::text, 'Absent'::text, 'Cancelled'::text, 'Holiday'::text])),
  "MagicLinkToken" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "GoogleEventID" text,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UserID" integer,
  CONSTRAINT tblsession_pkey PRIMARY KEY ("ID"),
  CONSTRAINT "TblSession_MagicLinkToken_key" UNIQUE ("MagicLinkToken"),
  CONSTRAINT class_sessions_recurring_schedule_id_fkey FOREIGN KEY ("RecurringScheduleID") REFERENCES public."TblRecurringSchedule"("ID"),
  CONSTRAINT class_sessions_module_id_fkey FOREIGN KEY ("ModuleID") REFERENCES public."TblModule"("ID"),
  CONSTRAINT class_sessions_semester_id_fkey FOREIGN KEY ("SemesterID") REFERENCES public."TblSemester"("ID"),
  CONSTRAINT "FK_TblSession_User" FOREIGN KEY ("UserID") REFERENCES public."TblUser"("UserID") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblSemesterDashboardSummary" (
  "SemesterID" bigint NOT NULL,
  "SemesterName" text NOT NULL,
  "StartDate" date NOT NULL,
  "EndDate" date NOT NULL,
  "SemesterHealthRate" double precision NOT NULL,
  "TodaySessionsCount" integer NOT NULL,
  "UpcomingSessionsCount" integer NOT NULL,
  "TotalSessions" integer NOT NULL,
  "PresentSessions" integer NOT NULL,
  "AbsentSessions" integer NOT NULL,
  "LateSessions" integer NOT NULL,
  "CancelledSessions" integer NOT NULL,
  "HolidaySessions" integer NOT NULL,
  "ValidSessions" integer NOT NULL,
  "CalculatedRate" double precision NOT NULL,
  "TodayAttendanceRate" double precision,
  "WarningsJson" text NOT NULL DEFAULT '[]'::text,
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UserID" integer,
  CONSTRAINT "TblSemesterDashboardSummary_pkey" PRIMARY KEY ("SemesterID"),
  CONSTRAINT "TblSemesterDashboardSummary_semester_id_fkey" FOREIGN KEY ("SemesterID") REFERENCES public."TblSemester"("ID") ON DELETE CASCADE,
  CONSTRAINT "FK_TblSemesterDashboardSummary_User" FOREIGN KEY ("UserID") REFERENCES public."TblUser"("UserID") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblNotification" (
  "NotificationID" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "UserID" integer NOT NULL,
  "SessionID" bigint,
  "Title" text NOT NULL,
  "Message" text NOT NULL,
  "NotificationType" text NOT NULL,
  "IsRead" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "TriggeredAt" timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT tblnotification_pkey PRIMARY KEY ("NotificationID"),
  CONSTRAINT "FK_TblNotification_User" FOREIGN KEY ("UserID") REFERENCES public."TblUser" ON DELETE CASCADE,
  CONSTRAINT "FK_TblNotification_Session" FOREIGN KEY ("SessionID") REFERENCES public."TblSession" ON DELETE CASCADE
);
