# ACST: Personal Attendance & Class Schedule Tracker

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-blue.svg)](https://www.postgresql.org/)
[![Blazor](https://img.shields.io/badge/Frontend-Blazor%20WASM-purple.svg)](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
[![Flutter](https://img.shields.io/badge/Mobile-Flutter-cyan.svg)](https://flutter.dev/)

**ACST** (Attendance & Class Schedule Tracker) is a specialized application designed to automatically organize class schedules, monitor attendance rates in real-time, and streamline attendance logging via Google Calendar integrations and passwordless magic links.

---

## 📌 Motivation & Purpose

This project was developed out of necessity: **I do not attend classes regularly and frequently have an attendance rate of less than 75%**.

To avoid academic penalties (such as being barred from exams or failing modules due to poor attendance), this tracker provides:
1. **Real-time visibility** into exactly where my attendance stands for each module.
2. **Predictive insights & visual indicators** (Green/Yellow/Red warning system) to help maintain an attendance rate of **$\ge 75\%$**.
3. **Frictionless logging** using **one-click "Mark as Present" magic links** sent directly to Google Calendar events.

---

## 🚀 Key Features

- **Semester & Module Management**: Define academic semesters (with specific start/end boundaries) and register courses/modules with instructors.
- **Recurring Schedule & Automation Engine**: Define weekly recurring patterns (e.g., Monday 9:00 AM - 11:00 AM) and auto-generate class sessions for the entire semester.
- **Google Calendar Synchronization**: 
  - Automatically create calendar events for all generated class sessions.
  - Inject custom, passwordless **"Mark as Present" magic links** directly into the Google Calendar descriptions.
- **Time-Window Enforced Attendance**: Magic links are valid only within a specific grace period (from 15 minutes before the class starts until 1 hour after it ends) to ensure honest logs.
- **Smart Holiday & Cancellation Handling**:
  - Automatically fetch/seed public holidays and tag sessions on holiday dates as `Holiday`.
  - Exclude holidays and manually cancelled sessions from the attendance denominator.
- **Analytics & Health Dashboard**:
  - Clear dashboard home indicating upcoming classes, daily schedules, and overall semester status.
  - Visual indicators:
    - 🟢 **Green ($\ge$ 75%)**: Safe zone.
    - 🟡 **Yellow (60% - 74%)**: Warning zone, need to attend upcoming sessions.
    - 🔴 **Red (< 60%)**: Critical failure zone.

---

## 📐 Attendance Calculation Formula

Holidays and cancelled sessions are fully excluded from statistics so that the rate only reflects the ratio of actual classes attended vs. actual classes held:

$$\text{Attendance Rate} = \frac{\text{Present Sessions}}{\text{Total Sessions} - (\text{Holidays} + \text{Cancelled})} \times 100\%$$

---

## 📂 Project Architecture

This repository is built using a **Vertical Slice-like (Feature-based) Organization** to ensure high cohesion and simplicity of features.

```
ACST/
├── ACST.Api/            # ASP.NET Core Web API (entry point, auth, routes)
├── ACST.Domain/         # Business logic (Services, Controllers, DTOs, Feature Slices)
├── ACST.Database/       # EF Core AppDbContext, migrations, and PostgreSQL models (Tbl*)
├── ACST.Shared/         # Shared helpers, pagination model, Result/Result<T> patterns
├── ACST.Domain.Tests/   # Unit testing suite (xUnit)
├── ACST.WebApp/         # Blazor WebAssembly client (Dashboard and Management)
└── ACST.Mobile/         # Flutter cross-platform mobile client application
```

---

## 🛠️ Technology Stack

- **Backend**: C# / .NET 10.0, EF Core (Npgsql PostgreSQL Provider)
- **Database**: PostgreSQL / Supabase
- **Frontend**: Blazor WebAssembly (WASM) with .NET 10 "Auto" rendering mode
- **Mobile Client**: Flutter / Dart
- **Background Jobs**: Hangfire (for holiday fetching and rolling schedules generation)
- **Integrations**: Google Calendar API

---

## 💻 Getting Started

### 📋 Prerequisites

- **.NET SDK**: 10.0 or higher
- **PostgreSQL**: Local instance or Supabase database URL
- **Flutter SDK**: For mobile client development
- **Google Calendar Credentials**: `client_secret.json` for OAuth calendar access

### 🗄️ Database Setup

1. Create a PostgreSQL database named `acst_db`.
2. Apply the schema using [db.sql](file:///d:/Practice/aspdotnetcore/attendance-tracker/ACST/db.sql):
   ```bash
   psql -h localhost -U postgres -d acst_db -f db.sql
   ```
3. Seed the sample data using [seed_test_data.sql](file:///d:/Practice/aspdotnetcore/attendance-tracker/ACST/seed_test_data.sql):
   ```bash
   psql -h localhost -U postgres -d acst_db -f seed_test_data.sql
   ```

### ⚙️ Backend Configuration

Update [appsettings.json](file:///d:/Practice/aspdotnetcore/attendance-tracker/ACST/ACST.Api/appsettings.json) under `ACST.Api/` with your PostgreSQL Connection String and Google Calendar API configurations:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=acst_db;Username=postgres;Password=YOUR_PASSWORD"
  },
  "GoogleCalendar": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

### 🏃 Running the Application

#### Run the Web API & Backend
```bash
dotnet run --project ACST.Api/ACST.Api.csproj
```

#### Run the Blazor Web Dashboard
```bash
dotnet run --project ACST.WebApp/ACST.WebApp.csproj
```

#### Run the Flutter Mobile App
```bash
cd ACST.Mobile
flutter run
```

#### Run Tests
```bash
dotnet test ACST.Domain.Tests/ACST.Domain.Tests.csproj
```

---

## 📜 Development Conventions

1. **Feature-based Isolation**: Colocate Controllers and Services inside features in `ACST.Domain/Features/[FeatureName]/`.
2. **Centralized DTOs**: Keep requests/responses in `ACST.Domain/DTOs/[FeatureName]/`.
3. **Result Pattern**: Services return a `Result<T>` or `Result` instead of throwing exceptions.
4. **Timezone Handling**: All server and database timestamps are stored in UTC, mapped to Myanmar Timezone on representation.
5. **Date Format**: Ensure dates display as `dd-mm-yyyy` and times in 12-hour format (`am`/`pm`).
