# ACST.Domain API Endpoints

This document lists all of the HTTP endpoints defined within the `ACST.Domain` project and details their routing, methods, and purpose.

---

## 1. Semesters
* **Controller:** [SemestersController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/Semesters/SemestersController.cs)
* **Base Route:** `api/Semesters`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/Semesters` | `GetAll` | Retrieves a paginated list of all semesters. |
| `GET` | `api/Semesters/{id}` | `GetById` | Retrieves a specific semester by its unique ID. |
| `POST` | `api/Semesters` | `Create` | Creates a new semester. |
| `PATCH` | `api/Semesters/{id}` | `Update` | Updates details of an existing semester. |
| `DELETE` | `api/Semesters/{id}` | `Delete` | Deletes a semester by its ID. |

---

## 2. Modules
* **Controller:** [ModulesController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/Modules/ModulesController.cs)
* **Base Route:** `api/Modules`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/Modules` | `GetAll` | Retrieves a paginated list of all modules, optionally filtered by semester ID. |
| `GET` | `api/Modules/{id}` | `GetById` | Retrieves a specific module by its unique ID. |
| `POST` | `api/Modules` | `Create` | Creates a new module. |
| `PUT` | `api/Modules/{id}` | `Update` | Replaces/updates details of an existing module by its ID. |
| `DELETE` | `api/Modules/{id}` | `Delete` | Deletes a module by its ID. |

---

## 3. Class Sessions
* **Controller:** [ClassSessionsController.cs (ClassSessionsController)](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/ClassSessions/ClassSessionsController.cs#L11)
* **Base Route:** `api/ClassSessions`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/ClassSessions` | `Get` | Retrieves a list of class sessions matching various query filters (semester, module, start/end dates, status, day of week, and pagination). |
| `GET` | `api/ClassSessions/{id}` | `GetById` | Retrieves a specific class session by its unique ID. |
| `POST` | `api/ClassSessions/generate` | `Generate` | Generates recurring class sessions based on a range and schedule. |
| `PATCH` | `api/ClassSessions/{id}/status` | `UpdateStatus` | Updates the status of a specific class session (e.g. Present, Absent, Cancelled). |
| `PATCH` | `api/ClassSessions/bulk-status` | `BulkUpdateStatus` | Bulk updates the status of multiple class sessions (e.g. to Present, Absent, Cancelled, or Not Marked). |
| `PUT` | `api/ClassSessions/{id}` | `Update` | Replaces/updates details of an existing class session by its ID. |
| `DELETE` | `api/ClassSessions/{id}` | `Delete` | Deletes a specific class session by its ID. |

---

## 4. Attendance (Public / Dashboard)
* **Controller:** [ClassSessionsController.cs (AttendanceController)](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/ClassSessions/ClassSessionsController.cs#L72)
* **Base Route:** `api/attendance`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/attendance/magic-link/{token}` | `MagicLink` | Marks attendance for a student using a secure magic link token, returning an Obsidian-style HTML success/error page. |
| `POST` | `api/attendance` | `MarkAttendance` | Marks or updates a class session's attendance status from the dashboard. |

---

## 5. Search
* **Controller:** [SearchController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/Search/SearchController.cs)
* **Base Route:** `api/Search`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/Search/modules` | `SearchModules` | Searches for modules matching specified query criteria with optional pagination and semester filtering. |
| `GET` | `api/Search/semesters` | `SearchSemesters` | Searches for semesters matching specified query criteria with optional pagination. |
| `GET` | `api/Search/sessions` | `SearchSessions` | Searches for class sessions matching specified query criteria with optional pagination, semester, and module filtering. |

---

## 6. Recurring Schedules
* **Controller:** [RecurringSchedulesController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/RecurringSchedules/RecurringSchedulesController.cs)
* **Base Route:** `api/modules/{moduleId}/recurring-schedules`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/modules/{moduleId}/recurring-schedules` | `GetByModule` | Retrieves all recurring schedules associated with a specific module. |
| `POST` | `api/modules/{moduleId}/recurring-schedules/{semesterId}` | `Create` | Creates a new recurring schedule for a module under a specific semester. |
| `DELETE` | `api/recurring-schedules/{id}` | `Delete` | Deletes a recurring schedule by its unique ID (overriding the base route via `~/api/recurring-schedules/{id}`). |

---

## 7. Holidays
* **Controller:** [HolidaysController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/Holidays/HolidaysController.cs)
* **Base Route:** `api/Holidays`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/Holidays` | `GetAll` | Retrieves a paginated list of all configured holidays. |
| `POST` | `api/Holidays` | `Create` | Creates a new holiday entry. |
| `POST` | `api/Holidays/seed` | `Seed` | Triggers a seed of holidays data (pre-populates standard holidays). |
| `DELETE` | `api/Holidays/{id}` | `Delete` | Deletes a holiday by its unique ID. |

---

## 8. Google Calendar Integration
* **Controller:** [GoogleCalendarController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/GoogleCalendar/GoogleCalendarController.cs)
* **Base Routes:** `api/google-auth` or `api/googlecalendar`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/google-auth/status`<br>`api/googlecalendar/status` | `GetStatus` | Checks whether the Google Calendar integration is enabled and whether the user is authenticated/connected. |
| `GET` | `api/google-auth/connect`<br>`api/googlecalendar/connect` | `Connect` | Initiates the Google OAuth authorization flow by generating and redirecting to the Google user consent URL. |
| `GET` | `api/google-auth/callback`<br>`api/googlecalendar/callback` | `Callback` | Handles the Google OAuth callback, exchanging the authorization code for access/refresh tokens and displaying a success page. |
| `POST` | `api/google-auth/disconnect`<br>`api/googlecalendar/disconnect` | `Disconnect` | Revokes and disconnects Google Calendar integration. |

*Note: The above Google Calendar integration endpoints are optional. They require the `GoogleCalendar:Enabled` API setting to be set to `true` (defaults to `false`) and client credentials to be configured. If disabled, calls to these endpoints will return a failure result.*

---

## 9. Analytics
* **Controller:** [AnalyticsController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Domain/Features/Analytics/AnalyticsController.cs)
* **Base Route:** `api/Analytics`

| HTTP Verb | Route | Method | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `api/Analytics/overall/{semesterId}` | `GetOverall` | Retrieves overall statistics and analytics summary for a given semester ID. |
| `GET` | `api/Analytics/modules/{moduleId}/{semesterId}` | `GetByModule` | Retrieves detailed, module-level analytics for a specific module and semester. |
| `GET` | `api/semesters/{id}/dashboard/summary` | `GetDashboardSummary` | Retrieves overall attendance rate for current semester & warnings. |
| `GET` | `api/semesters/{id}/dashboard/daily-weekly` | `GetDashboardDailyWeekly` | Retrieves day/week chart data. Supports an optional `month` query parameter (integer) to filter statistics for a specific month. |
| `GET` | `api/semesters/{id}/dashboard/modules` | `GetDashboardModules` | Retrieves module breakdown. Supports an optional `month` query parameter (integer) to filter statistics for a specific month. |
