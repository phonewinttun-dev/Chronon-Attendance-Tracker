
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
- Holidays are seeded into the database from Google Calendar Public Holiday API (or manual entry).
- During session generation, any session on a Holiday date gets `Status = Holiday`.
- Holidays can be viewed and managed in the UI.

**US-6:** As the user, I want to manually update session status so I can handle cancellations or corrections.
**Acceptance Criteria:**
- I can change the status of any individual session (Pending → Present/Absent/Late/Cancelled).
- Only the Status field is updated (efficient operation).
- Changing to `Cancelled` optionally updates the linked Google Calendar event.

---

#### Google Calendar Integration

**US-7:** As the user, I want class sessions to appear automatically in my Google Calendar.
**Acceptance Criteria:**
- After session generation, a corresponding event is created in Google Calendar.
- Event includes: Module name as title, correct time, and description with Magic Link.
- The Google `EventId` is stored in the database for future reference.
- Events are created with proper timezone handling.

**US-8:** As the user, I want a convenient Magic Link in Google Calendar events so I can mark attendance quickly.
**Acceptance Criteria:**
- The event description contains a clickable “Mark as Present” link with the unique session token.
- The link points to my application’s public endpoint.

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

**US-10:** As the user, I want to manually mark attendance from the dashboard.
**Acceptance Criteria:**
- Dashboard shows today’s / upcoming / past sessions.
- I can update status with one click (Present, Absent, Late, etc.).
- Changes are immediately reflected in analytics.

---

#### Analytics & Dashboard

**US-11:** As the user, I want clear attendance statistics so I can monitor my progress.
**Acceptance Criteria:**
- Attendance rate excludes Holidays and Cancelled sessions.
- Formula: `(Present) / (Total Valid Sessions) × 100` is correctly calculated.
- I can see per-module percentages and overall semester average.
- Rates ≥ 75% are shown in green; below 75% are flagged in red/warning color.
- I can filter and view detailed session list.

**US-12:** As the user, I want a clean home dashboard for quick overview.
**Acceptance Criteria:**
- Dashboard shows: Upcoming classes, Today’s schedule, Current semester attendance health.
- Visual warnings for low attendance.

---
