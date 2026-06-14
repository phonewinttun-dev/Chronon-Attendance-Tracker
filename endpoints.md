### 1. RESTful API Specifications

#### 1.1 Modules & Schedules

- `POST /api/modules`
  Create a new module (subject).
  **Body:** `{ name, teacherName? }`

- `GET /api/modules`
  List all modules.

- `GET /api/modules/{id}`
  Get details of a specific module.

- `PUT /api/modules/{id}`
  Update a module.

- `DELETE /api/modules/{id}`
  Delete a module (with confirmation / cascading checks).

- `POST /api/modules/{moduleId}/recurring-schedules`
  Create a recurring schedule (DayOfWeek, StartTime, EndTime) for a module in a semester.
  **Triggers** automatic or background generation of `ClassSessions`.

- `GET /api/modules/{moduleId}/recurring-schedules`
  List recurring schedules for a module.

- `POST /api/semesters` / `GET /api/semesters` / `PUT /api/semesters/{id}`
  CRUD for academic semesters.

#### 1.2 Class Sessions

- `GET /api/sessions`
  Retrieve class sessions with filters:
  Query params: `?startDate=...&endDate=...&moduleId=...&status=...&semesterId=...`

- `GET /api/sessions/{id}`
  Get a single session.

- `PATCH /api/sessions/{id}/status`
  Targeted update to change only the status (Pending, Present, Absent, Late, Cancelled, Holiday).
  **Body:** `{ status }`
  _Optionally triggers Google Calendar event update._

- `POST /api/sessions/generate`
  Manually trigger generation of class sessions for a semester (or specific module + semester).
  **Body:** `{ semesterId, moduleId? }`

- `POST /api/sessions/sync-calendar`
  Trigger incremental Google Calendar sync (using stored `NextSyncToken` if implementing full sync).

#### 1.3 Attendance Tracking

- `GET /api/attendance/magic-link/{token}`
  **Public webhook** for Magic Links from Google Calendar.
  - Validates token existence.
  - Enforces time window (15 min before → 1 hour after).
  - Applies `Present` status idempotently.
  - Returns user-friendly messages for success / already marked / outside window.

- `POST /api/attendance`
  Dashboard-based attendance marking.
  **Body:** `{ sessionId, status }`
  (Single-user → no `userId` needed)

#### 1.4 Holidays

- `GET /api/holidays`
  Retrieve all holidays (optionally filtered by date range).

- `POST /api/holidays/seed`
  Trigger fetching of public holidays from Google Calendar Public Holiday API and seed/update the database.

- `GET /api/holidays/{date}`
  Check if a specific date is a holiday.

#### 1.5 Analytics

- `GET /api/analytics/overall`
  Get overall semester attendance rate and statistics.

- `GET /api/analytics/modules/{moduleId}`
  Get detailed attendance rate and stats for a specific module.

- `GET /api/analytics/dashboard`
  Home dashboard summary (upcoming sessions, today’s classes, current attendance health, warnings).

---



---
