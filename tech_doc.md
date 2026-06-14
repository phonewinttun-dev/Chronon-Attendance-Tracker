### Personal Attendance & Class Schedule Tracker: Technical Document

### 1. Executive Summary

This document serves as the comprehensive technical blueprint for the Personal Attendance & Class Schedule Tracker. The solution leverages a modern technology stack featuring a .NET 10 backend, Blazor WebAssembly (WASM) for web interfaces, Flutter for mobile clients, and a PostgreSQL database. Key system capabilities include robust Google Calendar synchronization, passwordless "Magic Link" attendance tracking, automated recurring schedule generation, and high-performance attendance analytics. The architecture is designed to mitigate common scaling bottlenecks, ensure strict security standards, and provide seamless, synchronized data flow across multiple client platforms.

---

### 2. Technical Pitfall Analysis

**Google Calendar API Integration**

- **Pitfall:** Quota exhaustion from high-frequency polling and description fragility (injecting GUIDs into the user-facing `Description` field, which users can overwrite). Fetching full calendars repeatedly causes latency and bandwidth waste.
- **Best Practice:** Utilize `ExtendedProperties` (private or shared) to invisibly persist unique identifiers. Implement push notifications via the `calendar.events.watch` webhook rather than polling. Use `NextSyncToken` for incremental syncing to fetch only changed data, and employ `BatchRequest` to group API calls. Ensure `singleEvents=true` is set to expand recurring events for individual session mapping.

**Magic Link Attendance Marking**

- **Pitfall:** Token theft (AiTM), replay attacks via intercepted links, and unhandled double-clicks leading to duplicate records.
- **Best Practice:** Issue short-lived JWTs (5-15 minutes) bound to specific sessions. Implement strict Idempotency using `UPSERT` (`ON CONFLICT DO NOTHING`) logic paired with a unique database constraint `(session_id, user_id)`. Validate the time-window strictly in UTC (e.g., `-15` to `+60` minutes from start time) to enforce grace periods.

**Recurring Schedules & Database Growth (PostgreSQL)**

- **Pitfall:** Infinite record generation causing database bloat, lock contention from massive batch inserts, and drift between updated recurring rules and existing records.
- **Best Practice:** Implement bounded generation utilizing a rolling "Horizon" (e.g., generating records only for the next 6 months). Use PostgreSQL’s `generate_series()` function combined with `INSERT INTO ... SELECT` for optimized bulk generation. Store the RRULE pattern independently from physical session rows to allow for atomic `UPSERT` operations during schedule modifications.

**Attendance Percentages: SQL vs. C# Logic**

- **Pitfall:** Pulling millions of rows into C# memory to calculate simple percentages (N+1 query problems and memory bloat).
- **Best Practice:** Delegate data aggregations and streak tracking to PostgreSQL using **Window Functions** (`OVER(PARTITION BY student_id)`) and **CTEs**. Reserve C# Domain Layer logic exclusively for complex, procedural business rules (e.g., weighted attendance based on dynamic external factors) and final data formatting.

---

### 3. System Architecture Design

**Core Architecture Pattern: Vertical Slice Architecture (VSA)**
The .NET 10 backend eschews traditional layered architecture in favor of feature slices (e.g., `Features/Attendance`, `Features/Modules`). Each slice strictly encapsulates its API endpoint, business domain, and database access logic to ensure high cohesion and maintainability.

**Client Layer**

- **Blazor WASM (Web):** Utilizes .NET 10 "Auto" render mode. It performs Server-Side Rendering (SSR) for instantaneous initial load times, then transparently hydrates to WASM for rich client-side interactivity.
- **Flutter (Mobile):** Serves as the native mobile client. Consumes the identical API layer.

**API & Domain Layer (.NET 10)**

- **Idempotency Engine:** Middleware intercepts Magic Link attendance requests, returning a `200 OK` without database mutation if the attendance state is already valid.
- **Time-Window Validator:** Domain logic verifies UTC timestamps to authorize attendance strictly within the defined session grace periods.

**Data Layer (PostgreSQL)**

- **Bulk Generator:** Native SQL scripts execute `generate_series()` to materialize future sessions in single, highly efficient transactions.
- **Analytics Engine:** Aggregates real-time attendance statistics (excluding public holidays and cancelled sessions) directly at the query level via Window Functions.

**Background Workers (Hangfire)**

- **Holiday Seeding Worker:** Periodically fetches public holidays via Google Calendar and caches them in the `Holidays` table.
- **Bounded Schedule Generator:** Asynchronously expands stored RRULEs into physical `Session` records over the configured rolling time horizon.

---

### 4. Implementation Strategy

**Backend Foundation (.NET 10)**
The backend will utilize .NET 10 APIs grouped by Vertical Slice Architecture. This structure colocates handlers, requests, and domain models logically. EF Core with the Npgsql provider will be used to manage PostgreSQL interactions, leveraging raw SQL for highly specific `generate_series()` queries to ensure performance isn't lost to ORM overhead.

**Shared Contracts Pattern**
A dedicated `.NET 10` Class Library (e.g., `Attendance.Contracts`) will house all Data Transfer Objects (DTOs), Enums, and validation interfaces. This project will be referenced directly by both the Web API and the Blazor WASM client to guarantee compile-time type safety across the web ecosystem.

**Blazor WASM Integration**
The web interface will take advantage of .NET 10's "Auto" rendering mode. User requests will initially be served statically via the server for fast First Contentful Paint (FCP). The client will then download the WASM binaries in the background and transparently swap to client-side execution, utilizing the shared Contracts library for HTTP communication with the API.

**Flutter Mobile Integration**
To maintain parity with the .NET backend, OpenAPI (Swagger) documentation will be natively exposed by the .NET 10 API. The Flutter development pipeline will utilize code generation tools (e.g., `openapi_generator` for Dart) to automatically build the mobile API clients and strongly-typed Dart models. This ensures the Flutter app remains perfectly synchronized with backend contract modifications without manual translation.

**Backend for Frontend (BFF) Considerations**
If mobile (Flutter) and web (Blazor) UI workflows diverge significantly, a BFF routing layer will be introduced in the API gateway. This provides customized payload shaping per client type, preventing over-fetching on mobile devices while utilizing the same underlying domain services.

---
