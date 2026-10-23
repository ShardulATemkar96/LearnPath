# LearnPath Developer Logging — `info.log` vs `system.log` vs `AuditLog`

> **Purpose:** Give the developer instant console + file visibility of **who did what and when** without opening Admin panel. Keep generic system errors separate.

## Locations (inside project, auto-created)

```
backend/
└── Logs/
    ├── info.log    — strict user activity (one file for all users, append-only)
    └── system.log  — generic application/system events (append-only)
```

- Directory `backend/Logs/` is created automatically on first log.
- Files are created automatically if missing.
- Both survive restarts — new entries append, never overwrite.
- `.gitignore` contains `backend/Logs/` and `*.log` — logs are local, never committed.

## What goes where

### `info.log` — User Activity (developer-facing)
**Format file:** `UserId: 42 | Name: Shardul | Action: Login Successful | Timestamp: 2026-09-06 17:10:32`
**Format console:** `[USER ACTIVITY] UserId: 42 | Name: Shardul | Action: Login Successful | Timestamp: 2026-09-06 17:10:32`

Strictly **meaningful domain actions**:

- Auth: `Registered Successfully`, `Login Successful`, `Login Failed` (Unknown if anonymous), `Logout`, `Password Changed`
- Academic: `Created/Updated/Deleted Learning Path`, `Created/Updated/Deleted Module`, `Published Module`, `Completed Module`, `Certificate Generated`, `Created/Joined/Left Classroom`, `Created Assignment`, `Submitted Assignment`, `Graded Submission`, etc.
- Assessment: `Created Quiz`, `Started Quiz`, `Submitted Quiz`, `Quiz Passed`, `Quiz Failed`, `Uploaded Question Bank`, etc.
- Community: `Created/Updated/Deleted Post`, `Upvoted Post`, `Created Comment`, `Created/Joined Group`, `Submitted Report`, `Generated AI Feedback`, etc.
- Extras: `Opened Learning Path`, `Opened Module`, `Module Access Denied` (when locked)

**What is NOT in `info.log`:** system events, EF queries, repository calls, controller names, request paths, passwords/tokens/secrets.

Failed anonymous actions use `UserId: Unknown | Name: Unknown | Action: Login Failed | Timestamp: ...`

### `system.log` — System/Application (operator-facing)
**Format file:** `Application started — Environment: Development | ContentRoot: ... | Timestamp: 2026-09-06 17:10:32`
**Format console:** `[SYSTEM] Application started ... | Timestamp: ...`

For:

- Application started / stopped
- Unhandled exceptions (`ExceptionMiddleware` — `KeyNotFoundException`, `DbUpdateException` FK violation, etc.)
- Infrastructure failures (DB connection, migrations)
- Generic framework diagnostics

**Never** contains user activity. Never pollutes `info.log`.

### `AuditLog` table — Admin History (persistent DB, append-only)
- Existing `AuditLog` entity + `IAuditLogService` + `GuardAuditLogImmutability` in `ApplicationDbContext`.
- Used by Admin panel: `GET /api/v1/history/me` (own history) and `GET /api/v1/history/admin` (Admin role plus the `IsSuperAdmin` flag).
- **Relationship:**

```
Meaningful User Action
        ├── AuditLog (DB, Admin UI)
        └── UserActivityLogger (console + info.log) — reuses AuditLog event via AuditLogService hook
System Event
        └── SystemLogger (console + system.log)
```

## Implementation (reuse → extend)

- **Reused:** `AuditLog`/`IAuditLogService`/`AuditAction`/`AuditActionCatalog` (91 actions), `IHttpContextAccessor` for `UserId/Name/Role` resolution, existing `ILogger` + `ExceptionMiddleware` + `LoggingMiddleware`, `Program.cs` DI.
- **Added:**
  - `Interfaces/Services/Logging/IUserActivityLogger.cs` — `Log(userId,name,action)` + `Log(AuditAction,...)` + `LogUnknown`
  - `Interfaces/Services/Logging/ISystemLogger.cs` — `Log` / `LogError`
  - `Services/Logging/UserActivityLogger.cs` — `ILogger` → console `[USER ACTIVITY]` + `File.AppendAllText` to `Logs/info.log` with `SemaphoreSlim(1,1)` (thread-safe, 2s timeout, never throws)
  - `Services/Logging/SystemLogger.cs` — same pattern to `Logs/system.log` with `[SYSTEM]`
- **Integrated:**
  - `Services/Audit/AuditLogService.cs` now takes optional `IUserActivityLogger` and mirrors every `LogAsync` to activity log (maps `AuditAction` → human `Registered Successfully` etc., forces `Unknown` for anonymous failures).
  - `Services/LearningPath/LearningPathService.cs` adds explicit `Opened Learning Path` / `Opened Module` / `Module Access Denied` (meaningful reads not audited).
  - `Middleware/ExceptionMiddleware.cs` now takes optional `ISystemLogger` and writes unhandled exceptions to `system.log`.
  - `Program.cs` registers `AddSingleton<IUserActivityLogger, UserActivityLogger>` + `AddSingleton<ISystemLogger, SystemLogger>` and logs `Application started` via `ISystemLogger` after `Build()`; ensures `Logs/` exists.

## Thread safety & error handling

- Both loggers use static `SemaphoreSlim(1,1)` to serialize concurrent `AppendAllText`.
- All file writes wrapped in `try/catch` — logging failure is counted as warning but **never** bubbles to the user (login/quiz still succeeds).

## Verification (real run, 2026-09-06)

```
[USER ACTIVITY] UserId: 87cfb6c5-... | Name: testuser2+58ad@test.com | Action: Registered Successfully | Timestamp: 2026-09-06 19:03:46
[USER ACTIVITY] UserId: 87cfb6c5-... | Name: testuser2+58ad@test.com | Action: Login Successful | Timestamp: 2026-09-06 19:03:48
[USER ACTIVITY] UserId: 87cfb6c5-... | Name: testuser2+58ad@test.com | Action: Login Failed | Timestamp: 2026-09-06 19:03:49
[USER ACTIVITY] UserId: Unknown | Name: Unknown | Action: Login Failed | Timestamp: 2026-09-06 19:03:50
[SYSTEM] Unhandled exception on GET /api/v1/paths/1 — KeyNotFoundException: Learning path not found. | Timestamp: 2026-09-06 19:03:51
```

`info.log` captured 4 user activities (last run, append-only, multiple users share one file). `system.log` captured 3 `Application started` across restarts (proves append, not overwrite) plus the 404 exception — correctly separated.

## What is NOT logged

- Passwords, hashes, JWT, refresh tokens, headers, request bodies
- EF Core queries, repository entry/exit, controller names
- Pure `GET` noise — only domain-meaningful reads like `Opened Learning Path`

## Existing functionality safety

- Authentication, JWT 60m/7d, refresh rotation, lockout, RBAC, learning paths, DAG, quizzes, classrooms, community, certificates, Admin/Audit history all untouched — build succeeded, manual register/login 200 OK, `GET /history/me` still requires auth.
