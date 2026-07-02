CREATE TABLE public."TblSemester" (
  "Id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "Name" text NOT NULL,
  "StartDate" date NOT NULL,
  "EndDate" date NOT NULL,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT TblSemester_pkey PRIMARY KEY ("Id")
);

CREATE TABLE public."TblModule" (
  "Id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "Name" text NOT NULL,
  "ModuleCode" text NOT NULL,
  "TeacherName" text,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "SemesterId" bigint,
  CONSTRAINT TblModule_pkey PRIMARY KEY ("Id"),
  CONSTRAINT TblModule_semester_id_fkey FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id")
);

CREATE TABLE public."TblRecurringSchedule" (
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
  CONSTRAINT TblRecurringSchedule_pkey PRIMARY KEY ("Id"),
  CONSTRAINT recurring_schedules_module_id_fkey FOREIGN KEY ("ModuleId") REFERENCES public."TblModule"("Id") ON DELETE CASCADE,
  CONSTRAINT recurring_schedules_semester_id_fkey FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id") ON DELETE CASCADE
);

CREATE TABLE public."TblSession" (
  "Id" bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  "RecurringScheduleId" bigint NOT NULL,
  "ModuleId" bigint NOT NULL,
  "SemesterId" bigint NOT NULL,
  "SessionDate" date NOT NULL,
  "StartDatetime" timestamp with time zone NOT NULL,
  "EndDatetime" timestamp with time zone NOT NULL,
  "Status" text NOT NULL DEFAULT 'Not Marked'::text CHECK ("Status" = ANY (ARRAY['Not Marked'::text, 'Present'::text, 'Absent'::text, 'Cancelled'::text, 'Holiday'::text])),
  "MagicLinkToken" uuid NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
  "GoogleEventId" text,
  "IsDeleted" boolean NOT NULL DEFAULT false,
  "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT TblSession_pkey PRIMARY KEY ("Id"),
  CONSTRAINT class_sessions_recurring_schedule_id_fkey FOREIGN KEY ("RecurringScheduleId") REFERENCES public."TblRecurringSchedule"("Id") ON DELETE CASCADE,
  CONSTRAINT class_sessions_module_id_fkey FOREIGN KEY ("ModuleId") REFERENCES public."TblModule"("Id") ON DELETE CASCADE,
  CONSTRAINT class_sessions_semester_id_fkey FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id") ON DELETE CASCADE
);

CREATE TABLE public."TblSemesterDashboardSummary" (
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
  CONSTRAINT TblSemesterDashboardSummary_pkey PRIMARY KEY ("SemesterId"),
  CONSTRAINT TblSemesterDashboardSummary_semester_id_fkey FOREIGN KEY ("SemesterId") REFERENCES public."TblSemester"("Id") ON DELETE CASCADE
);
