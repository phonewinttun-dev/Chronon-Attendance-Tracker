**Product Requirements Document (PRD)**
**Personal Attendance & Class Schedule Tracker**

**Version:** 1.0
**Date:** June 2026
**Target User:** Single user (Personal use only)
**Platform:** .NET + Blazor (Web Dashboard)

---

### 1. Product Overview

This application helps you automatically manage your class schedule, track attendance, handle holidays and cancellations, and maintain an optional Google Calendar sync with one-click "Mark as Present" magic links.

The system supports recurring weekly classes, auto-generates individual sessions per semester, intelligently handles holidays, provides detailed bulk status management, and provides clear attendance analytics with month-level filtering.

---

### 2. Core Features

#### 2.1 Semester Management

- **Semester Lifecycle (CRUD)**
  - View, create, modify, and delete semesters.
  - Define academic periods (e.g., "UCSY 2025/2026 Semester 1") with Start Date and End Date. (dd-mm-yyyy)

#### 2.2 Module & Recurring Schedule Engine

- **Module Management**
  Create and manage academic modules/courses with the following details:
  - Module Name (required)
  - Teacher/Instructor Name (optional)

- **Recurring Schedule Definition**
  For each module in a semester, define a weekly recurring pattern:
  - Day of Week (Monday, Wednesday, etc.)
  - Start Time
  - End Time

- **Auto-Generation of Class Sessions**
  The system automatically generates individual class sessions for the entire semester based on the recurring schedule.
  Generation should respect the semester date range.

---

#### 2.2 Status Management & Holiday Handling

- **Session Status Enum**
  Each class session must have one of the following statuses:

$$\text{Status} \in \{\text{Present}, \text{Absent}, \text{Cancelled}, \text{Holiday}, \text{Not Marked}\}$$

  *Note: Generated class sessions default to `Not Marked` status unless they fall on a holiday, in which case they default to `Holiday`.*

- **Holiday Management**
  - Maintain a `Holidays` table seeded with public holidays.
  - Use the Google Calendar Public Holiday API to fetch and pre-populate holidays for the relevant country/region. *Note: Holiday fetching from Google Calendar is only supported when Google Calendar integration is explicitly enabled.*
  - During class session generation, if a session falls on a holiday date, its status is automatically set to `Holiday`.

- **Manual Overrides**
  - Admin (you) can manually update any session’s status (e.g., mark as `Cancelled` when a class is suddenly called off).
  - Updates should be efficient — only the `Status` field is modified.

---

#### 2.3 Google Calendar Integration (Optional & Configurable)

- **Optional Integration**
  Google Calendar integration is optional and configured via `GoogleCalendar:Enabled` settings (defaults to `false`).
  
- **Sync Behavior (When Enabled)**
  - When enabled, automatically create a corresponding Google Calendar event for every generated `ClassSession` and store the returned `EventId` in the database for future updates/deletes.
  - Authorization is performed via an OAuth connection flow in the UI.

- **Magic Link in Event (When Enabled)**
  - Each event’s description includes a unique, passwordless **"Mark as Present"** magic link containing a GUID token for that specific session.
  - Title: Module Name
  - Time: Exact session start/end
  - Description: Module details + clickable Magic Link + optional notes

- **Behavior When Disabled (Default)**
  - Google Calendar event creation, updates, and deletions are completely skipped.
  - The calendar connection controls and integration status settings are hidden from the UI.
  - Holiday fetching via the Google API is disallowed.

---

#### 2.4 Attendance Workflow & Validation

- **Magic Link (One-Click Attendance)**
  - Clicking the magic link in Google Calendar calls a public .NET API endpoint.
  - No login required.
  - The link contains a unique GUID token.

- **Time Window Enforcement**
  The magic link is only valid from **15 minutes before** the class starts until **1 hour after** it ends.
  Attempts outside this window return: _"Outside of attendance window."_

- **Idempotency**
  Multiple clicks on the same link are safely handled — the system returns _"Already marked as present."_ without duplicate updates.

- **Manual Marking via Dashboard**
  - View daily/weekly/monthly classes in the Blazor dashboard and update status via a vertical three-dot actions menu (replaces the quick action column) with a confirmation dialog flow.
  - **Bulk Status Management**: Select multiple class sessions on the Attendance page and perform bulk updates to mark them as `Present`, `Absent`, `Cancelled`, or `Not Marked` simultaneously with confirmation flow dialogs.

---

#### 2.5 Analytics & Health Dashboard

- **Attendance Calculation Rules**
  Holidays, Cancelled, and **Not Marked** (future or unrecorded) sessions are **completely excluded** from attendance rate calculations.

  **Formula:**

  $$
  \text{Attendance Rate} = \frac{\text{Present Sessions}}{\text{Total Sessions} - (\text{Holidays} + \text{Cancelled} + \text{Not Marked})} \times 100\%
  $$

- **Views & Filtering Required**
  - Per-module attendance percentage and total session counts.
  - Overall semester attendance percentage (average).
  - Granular list of all sessions with filters (by module, date range, status, day of week) on the Attendance page.
  - **Dynamic Month Filter**: Drill down and analyze stats and daily/weekly/monthly breakdowns for a specific month or overall.

- **Visual Indicators**
  - Show attendance rate with color coding:
    - Green (Safe): ≥ 75%
    - Yellow (Warning): 60% - 74%
    - Red (Critical): < 60%
  - Display "Not Marked" columns in breakdowns.

- **Dashboard Home**
  Overview of upcoming classes, today’s schedule, total sessions, and current semester health, supporting filtering by specific month.

---

### 3. Data Model (Recommended Schema Summary)

**Main Tables:**

- `Modules`
- `Semesters`
- `RecurringSchedules` (template)
- `ClassSessions` (individual instances — contains Status, MagicLinkToken, GoogleEventId, StartDateTime, EndDateTime, etc.)
- `Holidays`

**Key Design Notes:**

- Clear separation between recurring template and concrete sessions.
- `MagicLinkToken` is a unique GUID per session.
- Status is stored directly on `ClassSessions` (single-user simplification).
- All timestamps stored in UTC with proper timezone handling for Google Calendar. (Myanmar Timezone)
- Date format must be in dd-mm-yyyy & 12-hour time format (am/pm)

---

### 4. Non-Functional Requirements

- **Performance**: Fast generation of sessions (even for full semester ~100+ sessions).
- **Reliability**: Idempotent operations, proper error handling for Google API.
- **Security**: Magic link tokens are GUIDs (unguessable). Time-window validation on server.
- **Usability**: Clean, simple Blazor UI. Mobile-friendly.
- **Maintainability**: Clean .NET architecture (services, repositories, EF Core).
- **Offline/Graceful Failure**: Graceful handling if Google Calendar is temporarily unavailable.

---

### 5. Out of Scope (Current Version)

- Multi-user / student support
- Role-based access control
- Advanced recurrence patterns (e.g., bi-weekly, exceptions)
- Push notifications (beyond Google Calendar reminders)
- Export to PDF/Excel (can be added later)

---

### 6. Success Criteria

- Ability to define modules + recurring schedules and auto-generate all sessions accurately.
- Configurable and optional Google Calendar integration with working magic links when enabled.
- Correct attendance percentage that ignores Holidays, Cancelled, and Not Marked sessions.
- Intuitive dashboard showing clear progress, warnings, and dynamic month-level filtering.

---
