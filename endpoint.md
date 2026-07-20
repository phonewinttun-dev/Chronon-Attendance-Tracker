# ACST API Endpoints & RBAC Reference

This document lists all of the HTTP endpoints defined within the project, detailing their routing, methods, purpose, and required RBAC permissions.

> **Note on Admin Access**: Users with the `Admin` role automatically bypass all permission checks.

---

## 1. Authentication
* **Controller:** [AuthController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/AuthController.cs)
* **Base Route:** `api/Auth`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `api/Auth/register` | `Register` | *None (Public)* | Registers a new user account. |
| `POST` | `api/Auth/login` | `Login` | *None (Public)* | Authenticates user credentials and issues JWT access token & refresh token. |
| `POST` | `api/Auth/refresh-token` | `RefreshToken` | *None (Public)* | Rotates expired access token using a valid refresh token. |

---

## 2. Role & Permission Management
* **Controller:** [RolePermissionController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/RolePermissionController.cs)
* **Base Route:** `api/RolePermission`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/RolePermission/roles` | `GetRoles` | `Permissions.Roles.View` | Retrieves a list of all user roles. |
| `GET` | `api/RolePermission/permissions` | `GetPermissions` | `Permissions.Roles.View` | Retrieves a list of all system permissions. |
| `GET` | `api/RolePermission/roles/{roleId}/permissions` | `GetRolePermissions` | `Permissions.Roles.View` | Retrieves assigned permissions for a specific role. |
| `POST` | `api/RolePermission/roles/{roleId}/permissions` | `SetRolePermissions` | `Permissions.Roles.Manage` | Sets or updates permissions assigned to a role. |
| `POST` | `api/RolePermission/roles` | `CreateRole` | `Permissions.Roles.Manage` | Creates a new role. |

---

## 3. Semesters
* **Controller:** [SemestersController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/SemestersController.cs)
* **Base Route:** `api/Semesters`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/Semesters` | `GetAll` | `Permissions.Semesters.View` | Retrieves a paginated list of all semesters. |
| `GET` | `api/Semesters/{id}` | `GetById` | `Permissions.Semesters.View` | Retrieves a specific semester by its unique ID. |
| `POST` | `api/Semesters` | `Create` | `Permissions.Semesters.Create` | Creates a new semester. |
| `PATCH` | `api/Semesters/{id}` | `Update` | `Permissions.Semesters.Update` | Updates details of an existing semester. |
| `DELETE` | `api/Semesters/{id}` | `Delete` | `Permissions.Semesters.Delete` | Deletes a semester by its ID. |

---

## 4. Modules
* **Controller:** [ModulesController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/ModulesController.cs)
* **Base Route:** `api/Modules`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/Modules` | `GetAll` | `Permissions.Modules.View` | Retrieves a paginated list of all modules, optionally filtered by semester ID. |
| `GET` | `api/Modules/{id}` | `GetById` | `Permissions.Modules.View` | Retrieves a specific module by its unique ID. |
| `POST` | `api/Modules` | `Create` | `Permissions.Modules.Create` | Creates a new module. |
| `PUT` | `api/Modules/{id}` | `Update` | `Permissions.Modules.Update` | Replaces/updates details of an existing module by its ID. |
| `DELETE` | `api/Modules/{id}` | `Delete` | `Permissions.Modules.Delete` | Deletes a module by its ID. |

---

## 5. Class Sessions & Attendance
* **Controller:** [ClassSessionsController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/ClassSessionsController.cs)
* **Base Route:** `api/ClassSessions`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/ClassSessions` | `Get` | `Permissions.ClassSessions.View` | Retrieves a list of class sessions matching query filters. |
| `GET` | `api/ClassSessions/{id}` | `GetById` | `Permissions.ClassSessions.View` | Retrieves a specific class session by its unique ID. |
| `POST` | `api/ClassSessions/generate` | `Generate` | `Permissions.ClassSessions.Manage` | Generates recurring class sessions based on a range and schedule. |
| `PATCH` | `api/ClassSessions/{id}/status` | `UpdateStatus` | `Permissions.ClassSessions.Manage` | Updates the status of a specific class session. |
| `PATCH` | `api/ClassSessions/bulk-status` | `BulkUpdateStatus` | `Permissions.ClassSessions.Manage` | Bulk updates the status of multiple class sessions. |
| `PUT` | `api/ClassSessions/{id}` | `Update` | `Permissions.ClassSessions.Manage` | Replaces/updates details of an existing class session. |
| `DELETE` | `api/ClassSessions/{id}` | `Delete` | `Permissions.ClassSessions.Delete` | Deletes a specific class session by its ID. |
| `GET` | `api/ClassSessions/magic-link/{token}` | `MagicLink` | *None (Public)* | Marks attendance via single-click email link, returning styled HTML. |
| `POST` | `api/ClassSessions/attendance` | `MarkAttendance` | `Permissions.ClassSessions.Manage` | Marks or updates attendance status from the dashboard. |

---

## 6. Search
* **Controller:** [SearchController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/SearchController.cs)
* **Base Route:** `api/Search`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/Search/modules` | `SearchModules` | `Permissions.Search.View` | Searches for modules matching query criteria. |
| `GET` | `api/Search/semesters` | `SearchSemesters` | `Permissions.Search.View` | Searches for semesters matching query criteria. |
| `GET` | `api/Search/sessions` | `SearchSessions` | `Permissions.Search.View` | Searches for class sessions matching query criteria. |

---

## 7. Recurring Schedules
* **Controller:** [RecurringSchedulesController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/RecurringSchedulesController.cs)
* **Base Route:** `api/modules/{moduleId}/recurring-schedules`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/modules/{moduleId}/recurring-schedules` | `GetByModule` | `Permissions.RecurringSchedules.View` | Retrieves recurring schedules for a module. |
| `POST` | `api/modules/{moduleId}/recurring-schedules/{semesterId}` | `Create` | `Permissions.RecurringSchedules.Manage` | Creates a new recurring schedule for a module. |
| `DELETE` | `api/recurring-schedules/{id}` | `Delete` | `Permissions.RecurringSchedules.Manage` | Deletes a recurring schedule by ID (`~/api/recurring-schedules/{id}`). |

---

## 8. Holidays
* **Controller:** [HolidaysController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/HolidaysController.cs)
* **Base Route:** `api/Holidays`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/Holidays` | `GetAll` | `Permissions.Holidays.View` | Retrieves a paginated list of configured holidays. |
| `POST` | `api/Holidays` | `Create` | `Permissions.Holidays.Manage` | Creates a new holiday entry. |
| `POST` | `api/Holidays/import-google` | `ImportGoogleHolidays` | `Permissions.Holidays.Manage` | Imports holidays from Google Calendar. |
| `DELETE` | `api/Holidays/{id}` | `Delete` | `Permissions.Holidays.Manage` | Deletes a holiday by its unique ID. |

---

## 9. Google Calendar Integration
* **Controller:** [GoogleCalendarController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/GoogleCalendarController.cs)
* **Base Routes:** `api/google-auth` or `api/googlecalendar`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/google-auth/status`<br>`api/googlecalendar/status` | `GetStatus` | `Permissions.GoogleCalendar.Manage` | Checks whether integration is enabled and user is connected. |
| `GET` | `api/google-auth/connect`<br>`api/googlecalendar/connect` | `Connect` | `Permissions.GoogleCalendar.Manage` | Initiates Google OAuth authorization redirect. |
| `GET` | `api/google-auth/callback`<br>`api/googlecalendar/callback` | `Callback` | *None (OAuth Callback)* | Handles OAuth callback and token exchange. |
| `POST` | `api/google-auth/disconnect`<br>`api/googlecalendar/disconnect` | `Disconnect` | `Permissions.GoogleCalendar.Manage` | Revokes and disconnects Google Calendar integration. |

---

## 10. Analytics
* **Controller:** [AnalyticsController.cs](file:///d:/Practice/aspdotnetcore/attendance-tracker/Chronon-Attendance-Tracker/ACST.Api/Controllers/AnalyticsController.cs)
* **Base Route:** `api/Analytics`

| HTTP Verb | Route | Method | Required Permission | Description |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `api/Analytics/overall/{semesterId}` | `GetOverall` | `Permissions.Analytics.View` | Retrieves overall statistics for a semester ID. |
| `GET` | `api/Analytics/modules/{moduleId}/{semesterId}` | `GetByModule` | `Permissions.Analytics.View` | Retrieves module-level analytics. |
| `GET` | `api/semesters/{id}/dashboard/summary` | `GetDashboardSummary` | `Permissions.Analytics.View` | Retrieves dashboard summary for current semester & warnings. |
| `GET` | `api/semesters/{id}/dashboard/daily-weekly` | `GetDashboardDailyWeekly` | `Permissions.Analytics.View` | Retrieves day/week chart data (supports optional `month` query parameter). |
| `GET` | `api/semesters/{id}/dashboard/modules` | `GetDashboardModules` | `Permissions.Analytics.View` | Retrieves module breakdown chart data (supports optional `month` query parameter). |
