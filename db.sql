CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS public."TblRole" (
  "RoleId" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RoleName" text,
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblrole_pkey PRIMARY KEY ("RoleId")
);

CREATE TABLE IF NOT EXISTS public."TblPermission" (
  "PermissionId" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "PermissionName" text,
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblpermission_pkey PRIMARY KEY ("PermissionId")
);

CREATE TABLE IF NOT EXISTS public."TblRolePermission" (
  "RolePermissionId" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RoleId" integer,
  "PermissionId" integer,
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblrolepermission_pkey PRIMARY KEY ("RolePermissionId"),
  CONSTRAINT tblrolepermission_role_id_fkey FOREIGN KEY ("RoleId") REFERENCES public."TblRole"("RoleId"),
  CONSTRAINT tblrolepermission_permission_id_fkey FOREIGN KEY ("PermissionId") REFERENCES public."TblPermission"("PermissionId")
);

CREATE TABLE IF NOT EXISTS public."TblUser" (
  "UserId" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RoleId" integer,
  "FullName" text,
  "Email" text,
  "MobileNum" text,
  "PasswordHash" text,
  "CreatedAt" timestamp with time zone DEFAULT now(),
  "UpdatedAt" timestamp with time zone DEFAULT now(),
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tbluser_pkey PRIMARY KEY ("UserId"),
  CONSTRAINT tbluser_role_id_fkey FOREIGN KEY ("RoleId") REFERENCES public."TblRole"("RoleId")
);

CREATE TABLE IF NOT EXISTS public."TblUserToken" (
  "UserTokenId" integer GENERATED ALWAYS AS IDENTITY NOT NULL,
  "UserId" integer,
  "RefreshToken" text NOT NULL,
  "IsRevoked" boolean DEFAULT false,
  "ExpiresAt" timestamp with time zone,
  "CreatedAt" timestamp with time zone DEFAULT now(),
  "UpdatedAt" timestamp with time zone DEFAULT now(),
  "DeleteFlag" boolean DEFAULT false,
  CONSTRAINT tblusertoken_pkey PRIMARY KEY ("UserTokenId"),
  CONSTRAINT tblusertoken_user_id_fkey FOREIGN KEY ("UserId") REFERENCES public."TblUser"("UserId")
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
  "Id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "Name" text NOT NULL,
  "StartDate" date NOT NULL,
  "EndDate" date NOT NULL,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UserId" integer,
  CONSTRAINT tblsemester_pkey PRIMARY KEY ("Id"),
  CONSTRAINT "FK_TblSemester_User" FOREIGN KEY ("UserId") REFERENCES public."TblUser"("UserId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblModule" (
  "Id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "Name" text NOT NULL,
  "ModuleCode" text NOT NULL,
  "TeacherName" text,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "SemesterId" bigint,
  "UserId" integer,
  CONSTRAINT tblmodule_pkey PRIMARY KEY ("Id"),
  CONSTRAINT tblmodule_semester_id_fkey FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id"),
  CONSTRAINT "FK_TblModule_User" FOREIGN KEY ("UserId") REFERENCES public."TblUser"("UserId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblRecurringSchedule" (
  "Id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "ModuleId" bigint NOT NULL,
  "SemesterId" bigint NOT NULL,
  "DayOfWeek" smallint NOT NULL CHECK ("DayOfWeek" >= 0 AND "DayOfWeek" <= 6),
  "StartTime" time without time zone NOT NULL,
  "EndTime" time without time zone NOT NULL,
  "IsActive" boolean NOT NULL DEFAULT true,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UserId" integer,
  CONSTRAINT tblrecurringschedule_pkey PRIMARY KEY ("Id"),
  CONSTRAINT recurring_schedules_module_id_fkey FOREIGN KEY ("ModuleId") REFERENCES public."TblModule"("Id"),
  CONSTRAINT recurring_schedules_semester_id_fkey FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id"),
  CONSTRAINT "FK_TblRecurringSchedule_User" FOREIGN KEY ("UserId") REFERENCES public."TblUser"("UserId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblSession" (
  "Id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RecurringScheduleId" bigint NOT NULL,
  "ModuleId" bigint NOT NULL,
  "SemesterId" bigint NOT NULL,
  "SessionDate" date NOT NULL,
  "StartDatetime" timestamp with time zone NOT NULL,
  "EndDatetime" timestamp with time zone NOT NULL,
  "Status" text NOT NULL DEFAULT 'Not Marked'::text CHECK ("Status" = ANY (ARRAY['Not Marked'::text, 'Present'::text, 'Absent'::text, 'Cancelled'::text, 'Holiday'::text])),
  "MagicLinkToken" uuid NOT NULL DEFAULT uuid_generate_v4(),
  "GoogleEventId" text,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UserId" integer,
  CONSTRAINT tblsession_pkey PRIMARY KEY ("Id"),
  CONSTRAINT "TblSession_MagicLinkToken_key" UNIQUE ("MagicLinkToken"),
  CONSTRAINT class_sessions_recurring_schedule_id_fkey FOREIGN KEY ("RecurringScheduleId") REFERENCES public."TblRecurringSchedule"("Id"),
  CONSTRAINT class_sessions_module_id_fkey FOREIGN KEY ("ModuleId") REFERENCES public."TblModule"("Id"),
  CONSTRAINT class_sessions_semester_id_fkey FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id"),
  CONSTRAINT "FK_TblSession_User" FOREIGN KEY ("UserId") REFERENCES public."TblUser"("UserId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblSemesterDashboardSummary" (
  "SemesterId" bigint NOT NULL,
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
  "UserId" integer,
  CONSTRAINT "TblSemesterDashboardSummary_pkey" PRIMARY KEY ("SemesterId"),
  CONSTRAINT "TblSemesterDashboardSummary_semester_id_fkey" FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id") ON DELETE CASCADE,
  CONSTRAINT "FK_TblSemesterDashboardSummary_User" FOREIGN KEY ("UserId") REFERENCES public."TblUser"("UserId") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public."TblNotification" (
  "NotificationId" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "UserId" integer NOT NULL,
  "SessionId" bigint,
  "Title" text NOT NULL,
  "Message" text NOT NULL,
  "NotificationType" text NOT NULL,
  "IsRead" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "TriggeredAt" timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT tblnotification_pkey PRIMARY KEY ("NotificationId"),
  CONSTRAINT "FK_TblNotification_User" FOREIGN KEY ("UserId") REFERENCES public."TblUser" ON DELETE CASCADE,
  CONSTRAINT "FK_TblNotification_Session" FOREIGN KEY ("SessionId") REFERENCES public."TblSession" ON DELETE CASCADE
);
