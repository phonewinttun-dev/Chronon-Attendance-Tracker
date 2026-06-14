**Product Requirements Document (PRD)**
**Personal Attendance & Class Schedule Tracker**

**Version:** 1.0
**Date:** June 2026
**Target User:** Single user (Personal use only)
**Platform:** .NET + Blazor (Web Dashboard)

---

### 1. Product Overview

This application helps you automatically manage your class schedule, track attendance, handle holidays and cancellations, and maintain a Google Calendar sync with one-click "Mark as Present" magic links.

The system supports recurring weekly classes, auto-generates individual sessions per semester, intelligently handles holidays, and provides clear attendance analytics.

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

$$\text{Status} \in \{\text{Present}, \text{Absent}, \text{Cancelled}, \text{Holiday}\}$$

- **Holiday Management**
  - Maintain a `Holidays` table seeded with public holidays.
  - Use the Google Calendar Public Holiday API (or similar reliable source) to fetch and pre-populate holidays for the relevant country/region.
  - During class session generation, if a session falls on a holiday date, its status is automatically set to `Holiday`.

- **Manual Overrides**
  - Admin (you) can manually update any session’s status (e.g., mark as `Cancelled` when a class is suddenly called off).
  - Updates should be efficient — only the `Status` field is modified.

---

#### 2.3 Google Calendar Integration

- **Automatic Event Creation**
  For every generated `ClassSession`, automatically create a corresponding Google Calendar event.

- **Magic Link in Event**
  Each event’s description must include a unique **"Mark as Present"** magic link containing a GUID token for that specific session.

- **Event Details**
  - Title: Module Name
  - Time: Exact session start/end
  - Description: Module details + clickable Magic Link + optional notes
  - Store the returned Google `EventId` in the database for future updates/deletes.

- **Sync Behavior**
  - Create events during session generation.
  - Update or delete events when a session status changes to `Cancelled` (optional enhancement).

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
  View daily/weekly classes in the Blazor dashboard and update status with one click (Present, Absent, Late, etc.).

---

#### 2.5 Analytics & Health Dashboard

- **Attendance Calculation Rules**
  Holidays and Cancelled sessions are **completely excluded** from statistics.

  **Formula:**

  $$
  \text{Attendance Rate} = \frac{\text{Number of Present Sessions}}{\text{Total Sessions} - (\text{Holidays} + \text{Cancelled})} \times 100
  $$

- **Views Required**
  - Per-module attendance percentage
  - Overall semester attendance percentage (average)
  - Granular list of all sessions with filters (by module, date range, status)

- **Visual Indicators**
  - Show attendance rate with color coding:
    - Green: ≥ 75%
    - Yellow/Warning: < 75%
    - Red < 60%

- **Dashboard Home**
  Overview of upcoming classes, today’s schedule, and current semester health.

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
- Reliable Google Calendar events with working magic links.
- Correct attendance percentage that ignores Holidays and Cancelled classes.
- Intuitive dashboard showing clear progress and warnings.

---
