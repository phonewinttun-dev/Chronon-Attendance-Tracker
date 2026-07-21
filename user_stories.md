### User Stories & Acceptance Criteria

#### Module & Schedule Management

**US-1:** As the user, I want to create and manage Modules so that I can track different subjects/courses.
**Acceptance Criteria:**

- I can add a new Module with Name (required) and Teacher Name (optional).
- I can view, edit, and delete existing Modules.
- Modules are persisted in the database.

**US-2:** As the user, I want to define Semesters so that I can organize classes by academic period.
**Acceptance Criteria:**

- I can create a Semester with Name, Start Date, and End Date.
- I can view and edit existing Semesters.
- Semesters have clear date boundaries for session generation.

**US-3:** As the user, I want to set up recurring schedules for each Module so that classes repeat weekly.
**Acceptance Criteria:**

- For a Module in a specific Semester, I can define Day of Week, Start Time, and End Time.
- I can activate/deactivate recurring schedules.
- Multiple recurring schedules per Module are supported (e.g., Mon + Wed classes).

**US-4:** As the user, I want the system to auto-generate individual Class Sessions so I don’t have to create them manually.
**Acceptance Criteria:**

- Clicking “Generate Sessions” for a Semester creates one `ClassSession` record for every matching date within the Semester range.
- Generated sessions respect the recurring Day/Time pattern.
- Sessions falling on Holidays are automatically marked as `Holiday`.
- Generation is idempotent (re-running does not duplicate sessions).

---

#### Status & Holiday Management

**US-5:** As the user, I want Holidays to be automatically handled so attendance stats remain accurate.
**Acceptance Criteria:**

- When Google Calendar integration is enabled, holidays are seeded into the database from the Google Calendar Public Holiday API. If disabled, calendar holiday fetching is disallowed and the option is hidden from the UI.
- During session generation, any session on a Holiday date gets `Status = Holiday`.
- Holidays can be viewed in the UI.

**US-6:** As the user, I want to manually or bulk update session status so I can handle cancellations or corrections.
**Acceptance Criteria:**

- I can change the status of any individual session (Not Marked → Present/Absent/Cancelled) using a vertical three-dot actions menu with a confirmation dialog flow.
- I can select multiple sessions on the Attendance page and perform bulk updates to change their statuses simultaneously (to Present, Absent, Cancelled, or Not Marked) via a confirmation dialog flow.
- Only the Status field of the sessions is modified (efficient database operations).
- Changing to `Cancelled` optionally updates the linked Google Calendar event (when Google Calendar integration is enabled).

---

#### Google Calendar Integration (Optional)

**US-7:** As the user, I want class sessions to appear automatically in my Google Calendar when the integration is enabled.
**Acceptance Criteria:**

- Integration is optional and disabled by default. When `GoogleCalendar:Enabled` is true and valid credentials are provided, corresponding events are created in Google Calendar after session generation.
- Event includes: Module name as title, correct time, and description with Magic Link.
- The Google `EventId` is stored in the database.
- If integration is disabled, all background Google Calendar event synchronization is skipped, and calendar connection settings are hidden from the UI.

**US-8:** As the user, I want a convenient Magic Link in Google Calendar events so I can mark attendance quickly.
**Acceptance Criteria:**

- When Google Calendar integration is enabled, the event description contains a clickable "Mark as Present" link containing the unique session GUID token.
- The link points to the application's public endpoint.

---

#### Attendance Marking

**US-9:** As the user, I want to mark attendance via Magic Link without logging in.
**Acceptance Criteria:**

- Clicking the Magic Link calls the API with the token.
- The system validates the time window (15 min before start → 1 hour after end).
- If valid and first click → status changes to `Present`.
- If clicked again → shows “Already marked as present.”
- If outside time window → shows “Outside of attendance window.”
- All operations are idempotent.

**US-10:** As the user, I want to manually or bulk mark attendance from the dashboard/attendance views.
**Acceptance Criteria:**

- Dashboard and Attendance views show class sessions.
- I can update a session's status with confirmation dialogs.
- Changes are immediately reflected in analytics.

---

#### Analytics & Dashboard

**US-11:** As the user, I want clear attendance statistics so I can monitor my progress.
**Acceptance Criteria:**

- Attendance rate calculations exclude Holidays, Cancelled, and Not Marked sessions.
- Formula: `Present / (Total - (Holidays + Cancelled + Not Marked)) × 100` is correctly calculated.
- I can see per-module percentages, total session counts, and overall semester average.
- Rates ≥ 75% are shown in green; between 60% and 74% are yellow (warning); below 60% are flagged in red.
- I can filter and view the detailed session list on the Attendance page.

**US-12:** As the user, I want a clean home dashboard for quick overview with month-level filtering.
**Acceptance Criteria:**

- Dashboard shows: Upcoming classes, Today’s schedule, Total sessions, and current semester attendance health (with Not Marked breakdown columns).
- I can select a specific month via a dynamic month filter to drill down and analyze stats and breakdowns for only that month, or choose to view the overall semester stats.
- Visual warnings for low attendance.

---

#### Role & Permission Management

**US-13:** As an Admin, I want to view a list of all roles to manage role-based access.
**Acceptance Criteria:**

- I can view a list of all roles.
- The list includes role name.

**US-14:** As an Admin, I want to create a new role to define custom access levels.
**Acceptance Criteria:**

- I can create a new role with name.
- The new role is added to the list of roles.

**US-15:** As an Admin, I want to view a list of all available permissions to understand system access capabilities.
**Acceptance Criteria:**

- I can view a list of all available permissions.
- The list includes permission name.

**US-16:** As an Admin, I want to view permissions assigned to a specific role to review its access scope.
**Acceptance Criteria:**

- I can view permissions assigned to a specific role.
- The list includes permission name.

**US-17:** As an Admin, I want to set or update permissions for a role to control what users in that role can do.
**Acceptance Criteria:**

- I can set or update permissions for a role.
- The changes are immediately reflected in the system.

---

#### Authentication & Authorization

**US-18:** As an user, I want to create my own account by registering.
**Acceptance Criteria:**

- I can create a new account by registering.

**US-19:** As an active user, I want my session to remain open automatically so that my work isn't interrupted by frequent login prompts.

**Acceptance Criteria:**

- Upon successful login, the frontend extracts the expiration time (exp claim) from the decoded JWT Access Token.

- The frontend sets a background timer to trigger the refresh process proactively (2 minute before the actual expiration time).

- When the timer triggers, the system silently calls the refresh API endpoint using the Refresh Token without interrupting the user's current actions.

- Upon receiving the new Access Token, the system updates the token in frontend storage and resets the proactive timer based on the new expiration time.

- If the background refresh API call fails (e.g., the refresh token itself is expired, invalid, or there is a network error), the system completely clears the session and redirects the user to the login page with a "Session Expired" message.

**US-20:** As an Admin, I want all my actions to bypass permission restrictions so I can manage the system without hindrance.
**Acceptance Criteria:**

- I can bypass permission restrictions to manage the system.
- The changes are immediately reflected in the system.

---

#### Notification System

**US-21:** As an user, I want to receive notifications for upcoming classes so that I don't miss them.
**Acceptance Criteria:**

- I can receive notifications for upcoming classes.
- The notifications are displayed in the UI.
- The notifications are displayed in the correct time.

**US-22:** As an user, I want to receive notifications which remind me to fill up attendance.
**Acceptance Criteria:**

- The reminder is triggered 30 minutes after the class has ended.
- The reminder is triggered only once for each class.
- The reminder is triggered every evening at 6:30 PM server time.
- The reminder is displayed in the UI.
- The reminder is displayed only for classes which are not marked as present or absent.

---
