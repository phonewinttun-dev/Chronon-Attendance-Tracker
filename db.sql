CREATE TABLE public.TblModule (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  name text NOT NULL,
  teacher_name text,
  is_deleted boolean NOT NULL DEFAULT false,
  created_at timestamp with time zone NOT NULL DEFAULT now(),
  updated_at timestamp with time zone NOT NULL DEFAULT now(),
  semester_id bigint,
  CONSTRAINT TblModule_pkey PRIMARY KEY (id),
  CONSTRAINT TblModule_semester_id_fkey FOREIGN KEY (semester_id) REFERENCES public.TblSemester(id)
);
CREATE TABLE public.TblSemester (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  name text NOT NULL,
  start_date date NOT NULL,
  end_date date NOT NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  created_at timestamp with time zone NOT NULL DEFAULT now(),
  updated_at timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT TblSemester_pkey PRIMARY KEY (id)
);
CREATE TABLE public.TblRecurringSchedule (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  module_id bigint NOT NULL,
  semester_id bigint NOT NULL,
  day_of_week smallint NOT NULL CHECK (day_of_week >= 0 AND day_of_week <= 6),
  start_time time without time zone NOT NULL,
  end_time time without time zone NOT NULL,
  is_active boolean NOT NULL DEFAULT true,
  is_deleted boolean NOT NULL DEFAULT false,
  created_at timestamp with time zone NOT NULL DEFAULT now(),
  updated_at timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT TblRecurringSchedule_pkey PRIMARY KEY (id),
  CONSTRAINT recurring_schedules_module_id_fkey FOREIGN KEY (module_id) REFERENCES public.TblModule(id),
  CONSTRAINT recurring_schedules_semester_id_fkey FOREIGN KEY (semester_id) REFERENCES public.TblSemester(id)
);
CREATE TABLE public.TblSession (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  recurring_schedule_id bigint NOT NULL,
  module_id bigint NOT NULL,
  semester_id bigint NOT NULL,
  session_date date NOT NULL,
  start_datetime timestamp with time zone NOT NULL,
  end_datetime timestamp with time zone NOT NULL,
  status text NOT NULL DEFAULT 'Not Marked'::text CHECK (status = ANY (ARRAY['Not Marked'::text, 'Present'::text, 'Absent'::text, 'Cancelled'::text, 'Holiday'::text])),
  magic_link_token uuid NOT NULL DEFAULT uuid_generate_v4() UNIQUE,
  google_event_id text,
  is_deleted boolean NOT NULL DEFAULT false,
  created_at timestamp with time zone NOT NULL DEFAULT now(),
  updated_at timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT TblSession_pkey PRIMARY KEY (id),
  CONSTRAINT class_sessions_recurring_schedule_id_fkey FOREIGN KEY (recurring_schedule_id) REFERENCES public.TblRecurringSchedule(id),
  CONSTRAINT class_sessions_module_id_fkey FOREIGN KEY (module_id) REFERENCES public.TblModule(id),
  CONSTRAINT class_sessions_semester_id_fkey FOREIGN KEY (semester_id) REFERENCES public.TblSemester(id)
);
CREATE TABLE public.TblHoliday (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  holiday_date date NOT NULL UNIQUE,
  name text NOT NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  created_at timestamp with time zone NOT NULL DEFAULT now(),
  CONSTRAINT TblHoliday_pkey PRIMARY KEY (id)
);
