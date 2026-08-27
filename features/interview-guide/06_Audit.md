# 06 — Audit Logging

> Verified against the actual LearnPath backend and frontend source code.

---

## 1. What Is It?

Audit logging is a **permanent record of important actions** in the system: who did what,
when, and to which thing — with the values before/after where it matters.

Simple answer: *"Audit logging is an append-only trail of user actions so we can later answer
'who did what and when'."*

---

## 2. Why Is It Used?

LearnPath is an education platform where actions matter: logins, quiz attempts, role changes,
deactivated users, created content. Admin and instructors need to **prove** and **review** what
happened:

- Catch suspicious behavior (e.g., failed login attempts).
- Give students their own read-only activity history.
- Provide the Super Admin a system-wide history.
- Keep a record that normal application data can't supply (a user row gets edited — the audit row shows *before* vs *after*).

Audit data is different from application data: it must never be silently changed or deleted —
that would make the trail untrustworthy.

---

## 3. How Is It Used in LearnPath?

### Files

| File | Role |
|---|---|
| `backend/Entities/AuditLog.cs` | The immutable record |
| `backend/Entities/AuditAction.cs` | Enum of ~90 action types (LOGIN, QUIZ_STARTED, ROLE_CHANGED…) |
| `backend/Services/Audit/AuditLogService.cs` | Writes + reads audit rows; resolves "who is acting" |
| `backend/Services/Audit/AuditActionCatalog.cs` | Maps action → name + category for the UI |
| `backend/Controllers/HistoryController.cs` | Read-only REST access (own history + super admin) |
| `frontend/src/pages/history/HistoryPage.tsx` | User's own history UI (timeline + filters) |
| `frontend/src/pages/admin/AdminHistoryPage.tsx` | Super Admin system-wide history UI |

### How records are created

Services call `IAuditLogService.LogAsync(...)` after (or around) an important business action.
Examples seen in the source:

| Action | Where it's logged |
|---|---|
| `LOGIN` / `LOGIN_FAILED` / `LOGOUT` | `AuthService.LoginAsync`, `LogLoginFailedAsync`, `RevokeTokenAsync` |
| `QUIZ_STARTED` / `QUIZ_COMPLETED` / `QUIZ_PASSED` / `QUIZ_FAILED` | `AttemptService` |
| `QUIZ_CREATED`/`PUBLISHED`/`DELETED`/`LINKED_TO_MODULE` | `QuizService` |
| `QUESTION_BANK_UPLOADED` / `VERSION_UPLOADED` / `ARCHIVED` | `QuestionBankService` |
| `CLASSROOM_CREATED` / `JOINED` / `LEFT` / `MEMBER_REMOVED` | `ClassroomService` |
| `LEARNING_PATH_CREATED` / `MODULE_PUBLISHED` / `DEPENDENCY_ADDED` | `LearningPathService` |
| `ROLE_CHANGED` / `INSTRUCTOR_ASSIGNED` / `USER_DEACTIVATED` / `USER_ACTIVATED` | `AdminService` |
| `POST_CREATED` / `COMMENT_CREATED` | `CommunityService` |
| `SUBMISSION_UPLOADED` / `GRADED` / `STATUS_CHANGED` | `ClassroomService` |

(Representative examples only — the enum has ~90 values. Say this, don't list all of them.)

---

## 4. How It Works Internally

Writing a record:

```
Some service does an action (e.g. login)
  → await _auditLog.LogAsync(
        AuditAction.LOGIN,       // which event
        "User",                  // entity type
        user.Id,                 // entity id
        "User 'x' logged in.",   // human description
        oldValue? / newValue?,   // before/after where useful
        additionalData?,         // extra context, e.g. ModuleId
        userId?, username?, role?)   // can be omitted

  → AuditLogService.LogAsync
      → ResolveActor(userId, username, role)
          → fills gaps from the HttpContext's User claims:
              if userId   empty → ClaimTypes.NameIdentifier
              if username empty → ClaimTypes.Email / NameIdentifier
              if role     empty → first ClaimTypes.Role
          (this means services can log actions without knowing who called — the JWT does.)
      → new AuditLog(actionType, entityType, entityId, description, oldValue, newValue,
                     additionalData, actor.UserId, actor.Username, actor.Role)
          · constructor sets Timestamp = DateTime.UtcNow
      → _context.AuditLogs.AddAsync(...) ; SaveChangesAsync()
```

Reading (own history):

```
HistoryController.GetMyHistory (GET /history/me)
  → AuditLogService.GetMyHistoryAsync(userId, query)
      → filter: a.UserId == userId          // hard-coded, can't be widened
      → optional filters: ActionType, FromDate, ToDate, Search (Description contains)
      → sort newest first (Timestamp desc, then Id desc)
      → paginate (Page, PageSize ≤ 100) → map to DTO (adds ActionName + Category from catalog)
```

Super Admin view:

- `CanAccessAdminHistoryAsync` checks the `IsSuperAdmin` flag (not just the Admin role).
- `GetAdminHistoryAsync` adds filters for UserId/Role/EntityType and also computes whether each
  referenced entity still exists (`ResolveEntityExistenceAsync`) — so history can show
  "entity was deleted".

---

## 5. Important Internal Logic

- **Immutability:** `AuditLog` has **private setters** and no public parameterless setter for
  values. `Timestamp`, `UserId`, action, description etc. can only be set through the constructor.
  There is no update/delete endpoint in `HistoryController` — history is **append-only** by design
  (the controller comment says so explicitly).
- **Actor resolution:** `ResolveActor` reads the JWT claims from `IHttpContextAccessor` when the
  caller didn't pass user info — so even a service that doesn't know the user can record "who"
  reliably. Failed logins use `userId ?? "unknown"` since the user may not exist.
- **Failure isolation:** audit failures don't break the main operation — e.g. after deleting a
  learning path, `DeleteAsync` wraps the audit call in try/catch and logs the error with ILogger.
- **Paging caps:** page size clamped to 100; page minimum 1.
- **Date filtering:** `FromDate` → `>=` start of day; `ToDate` → `<` next day (UTC aware).
- **Existence resolution:** for the admin view, one batched query per entity type
  (`User`, `LearningPath`, `Classroom`, `Group`, `Post`, `Quiz`) to show whether the entity still exists.
- **Catalog:** `AuditActionCatalog` provides display `ActionName` + `Category` for the frontend.

---

## 6. Frontend Side

User history page (`/history`):

```
HistoryPage
  → on mount: historyService.getActionTypes()  → dropdown grouped by category (Authentication,
     Account, Academic, Assessment, Community…)
  → loadHistory → historyService.getMy({ page, pageSize, search, actionType, fromDate, toDate })
      → debounced search (400ms), filter changes reset page to 1
      → renders a vertical timeline (cards) with colored category dots, action name,
         description, entity type/id, timestamp
      → PaginationBar for paging (20/page)
```

Admin history page (`/history/admin`):

```
AdminHistoryPage → historyService.getAdminAccess() (IsSuperAdmin gate) → getAdmin(query)
  → extra filters: user search, role, entity type → table/grid view with EntityExists indicator
```

Note: history endpoints take **no user id** — the backend derives the user from the JWT, so one
user can't read another user's history through `GET /history/me`.

---

## 7. Backend Side

- Controller: `HistoryController` — all endpoints `[Authorize]`; admin endpoints also `[Authorize(Roles="Admin")]` (the service re-checks `IsSuperAdmin`).
- Service: `AuditLogService` (registered `AddScoped<IAuditLogService, AuditLogService>`).
- DI helper: `IHttpContextAccessor` for actor resolution.
- Endpoints:
  - `GET /history/me` — own history (paged/filtered)
  - `GET /history/action-types` — catalog options
  - `GET /history/admin/access` — can this user see admin history?
  - `GET /history/admin` — super-admin global history
  - `GET /history/admin/entity-types` — distinct entity types for filters

---

## 8. Database Side

Entity: `AuditLog` → table `AuditLogs`.

| Column | Meaning |
|---|---|
| `Id` | PK (long) |
| `Timestamp` (UTC) | when it happened — set in the constructor |
| `UserId`, `Username`, `Role` | who did it (from claims) |
| `ActionType` | enum (int) from `AuditAction` |
| `EntityType`, `EntityId` | what it touched (e.g. "Quiz", "42") |
| `Description` | human-readable summary |
| `OldValue`, `NewValue` | before/after values |
| `AdditionalData` | extra context (ModuleId, reason, version…) |

Relationship: standalone — audits reference entities by type+id strings, not FK constraints
(because the entity may be deleted later). This is deliberate: it keeps history intact even if
the source row is gone.

---

## 9. Security / Authorization

- `GET /history/me` returns **only** rows where `a.UserId == userId` (enforced in the service; can't be widened by any caller).
- Admin endpoints are gated by the Admin role **and** the service re-checks `IsSuperAdmin` on every call.
- `GetEntityTypesAsync` requires Super Admin.
- There are no write/update/delete history endpoints at all — read-only.

---

## 10. Simple Real Example

A student starts and finishes "Java Basics Quiz":

1. `AttemptService.StartAttemptAsync` → `LogAsync(QUIZ_STARTED, "Quiz", "12", "User started quiz 'Java Basics' (attempt #1).", additionalData: "ModuleId: 5")`. Actor (userId, email, role Student) is pulled from the JWT.
2. Student submits → `QUIZ_COMPLETED` then `QUIZ_PASSED` rows are written with the percentage.
3. The student later opens `/history` — `GetMyHistoryAsync` filters by their id, sorts newest-first, and shows the timeline: passed the quiz, started the quiz, logged in.
4. Meanwhile the Super Admin's `/history/admin` shows the same rows with `EntityExists: true` for the quiz.

---

## 11. Interview Answer

> "In LearnPath I built an audit logging system. When important actions happen — like login, quiz
> started or completed, role changes, or content created — the service calls
> `IAuditLogService.LogAsync` and writes an `AuditLog` row. It records the action type, entity, a
> description, old/new values, and who did it; the actor is resolved automatically from the JWT
> claims using `IHttpContextAccessor`, so services don't have to pass the user around. The
> `AuditLog` entity has private setters and there are no update or delete endpoints, so the trail
> is append-only. Every user gets their own read-only history page, and the Super Admin can see
> the whole system's history. I also made audit failures non-fatal so logging problems never
> break the actual operation."

(≈ 40–50 seconds)

---

## 12. Follow-Up Questions

**Q: Why is audit data separate from normal application data?**
Application data is meant to be edited; audit data must stay a permanent, untampered record. If the quiz title changes, the app row changes but the audit history shows the original action as it happened.

**Q: How do you prevent audits from being edited or deleted?**
The `AuditLog` entity only allows values through its constructor (private setters), and there are no update/delete history endpoints. It's append-only at both the code and the API level.

**Q: Who can see other users' history?**
Only the Super Admin — `GetAdminHistoryAsync` re-checks `IsSuperAdmin` inside the service, and the `/history/me` endpoint always filters by the JWT user id.

**Q: What actions are logged?**
About 90 event types in the `AuditAction` enum. Representative ones: `LOGIN`, `LOGIN_FAILED`, `LOGOUT`, `QUIZ_STARTED`, `QUIZ_COMPLETED`, `QUIZ_PASSED`, `QUIZ_FAILED`, `ROLE_CHANGED`, `USER_DEACTIVATED`, `POST_CREATED`, `CLASSROOM_JOINED`, `DEPENDENCY_ADDED`.

**Q: What if the write fails?**
It's best-effort. The service callers (e.g. `LearningPathService.DeleteAsync`) catch audit exceptions and log them separately so the primary operation still succeeds.

**Q: How is "who" resolved?**
`ResolveActor` fills `UserId`, `Username`, `Role` from the JWT claims on the current HTTP request when the caller didn't pass them. Failed logins use `"unknown"` since there may be no user.

**Q: What if the referenced entity is deleted later?**
Audit rows reference entities as `EntityType` + `EntityId` strings, not foreign keys, so they survive the deletion. `ResolveEntityExistenceAsync` even reports whether the entity still exists in the admin view.

**Q: Limitations / how to improve?**
Logging is done manually at call sites (a service could forget one). Improvements: centralize it with an interceptor/AOP or middleware that diffs tracked entities automatically, and add retention/archiving policies for large histories.

---

## 13. Functionality

Confirmed from source code:

- Append-only `AuditLog` entity with immutable constructor values (Timestamp set to `DateTime.UtcNow`).
- ~90 actions in the `AuditAction` enum including LOGIN, LOGIN_FAILED, LOGOUT, USER_CREATED, USER_DEACTIVATED, ROLE_CHANGED, QUIZ_STARTED/COMPLETED/PASSED/FAILED, QUESTION_BANK_UPLOADED, CLASSROOM_*, SUBMISSION_*, POST_CREATED, DEPENDENCY_ADDED, etc.
- `AuditLogService.LogAsync` with automatic actor resolution from JWT claims via `IHttpContextAccessor`.
- `GetMyHistoryAsync` (own, filtered, paged, newest first), `GetAdminHistoryAsync` (Super Admin only), `CanAccessAdminHistoryAsync`, `GetEntityTypesAsync`, `GetActionOptions`.
- `AuditActionCatalog` for friendly names + categories.
- Entity-existence resolution for admin history (`ResolveEntityExistenceAsync`).
- Read-only `HistoryController` with no write/update/delete routes.
- Frontend user history timeline with filters + pagination (`HistoryPage.tsx`) and admin history page (`AdminHistoryPage.tsx`).