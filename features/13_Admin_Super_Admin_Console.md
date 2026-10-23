# Admin Console (with Super Admin History Flag)

## 1. Feature Name

Admin Console (with Super Admin History Flag)

## 2. Purpose

- **Problem Solved:** Platform operators need central oversight: user administration (read-only role display and statuses), platform statistics, learning-path visibility, certificate management, and classroom management.
- **Why It Exists:** The `Admin` role gates a controller with full user/classroom/certificate CRUD. The `IsSuperAdmin` flag additionally gates global audit-history browsing; it is not a third role.
- **Role in Application:** It is the governance/operations subsystem for the whole platform, layered on top of all other domains.

---

## 3. What the User Can Do

### Admin
- View platform stats: total users, learning paths, classrooms, certificates issued, modules completed, new users this month, and a 6-month user-growth series.
- List and search all users; view a single user's full profile + counters.
- Search/filter/paginate certificates (search by name/email/path title; filter by issue-date range).
- View a certificate detail and delete certificates.
- Search/filter/paginate classrooms (search by title/code/path/creator; filter by learning path and status).
- View classroom detail (members, statuses) and edit classroom title/description.
- Reassign a classroom to a new learning path or transfer classroom ownership.
- Delete classrooms and users.
- Mark users invalid (requires a reason), activate/deactivate users.
- View all learning paths.
- View each user's roles read-only (Admin / Student). There is no role-update UI or API.

### Super Admin flag
- Everything an Admin can do, **plus** access to the global audit-history view. The flag does not create a separate role and does not permit role assignment.

---

## 4. Feature Workflow

```
[Admin opens console] → GET /api/v1/admin/stats
  → aggregates platform-wide counts + 6-month growth series

[Admin manages users]
  → GET /admin/users?search= → read-only role/status/counters per user
  → PUT /admin/users/{id}/invalid      {reason}
  → PUT /admin/users/{id}/activate | /deactivate
  → DELETE /admin/users/{id}           (soft delete + revoke tokens)

[Admin manages certificates]
  → GET /admin/certificates?search&fromDate&toDate&page&pageSize
  → GET /admin/certificates/{id} → DELETE /admin/certificates/{id}

[Admin manages classrooms]
  → GET /admin/classrooms?search&learningPathId&status&page&pageSize
  → GET /admin/classrooms/{id} → PUT /admin/classrooms/{id}
  → PUT /admin/classrooms/{id}/learning-path
  → DELETE /admin/classrooms/{id}
```

---

## 5. How It Works Internally

### Platform Stats (`GetStatsAsync`)
Counts users, learning paths, classrooms, certificates, and completed progresses (`Progresses.IsCompleted`). `NewUsersThisMonth` counts users created since the 1st of the current month. `UserGrowth` builds a 6-month series by looping `i` from 5 down to 0: for each month start (`YY-MM`), counts users with `CreatedAt in [start, start+1month)` and labels the entry with `start.ToString("MMM")`.

### User Listing (`GetAllUsersAsync`)
- Optional search over email, username, first/last name (substring).
- Sorted `CreatedAt` desc.
- Last-login heuristic: `RefreshTokens` grouped by `UserId`, taking `Max(CreatedAt)` per user, joined into a dictionary — the latest refresh-token issuance approximates the last login.
- Per-user DTO (`BuildUserDtoAsync`) adds live counters: paths created, modules completed, quiz attempts, certificates, classrooms joined.

### Read-Only Roles
- The only valid role values are `Admin` and `Student`.
- `UpdateUserRoleAsync`, the role-update endpoint, and the role-update UI were removed. Roles are returned in user DTOs for display only.
- Registration always creates a `Student`; the seed process creates the one `Admin` account. Neither path accepts a caller-supplied role.
- Historical `ROLE_CHANGED` audit rows remain readable, but no new role-change events are produced.

### Mark Invalid / Activate / Deactivate (`MarkInvalidAsync` / `ActivateUserAsync` / `DeactivateUserAsync`)
- **MarkInvalid:** allowed for any `Admin` actor (checked via `IsInRoleAsync`). The seed Super Admin account is protected. A non-empty `Reason` is required. Sets `Status = Invalid`, stores `InvalidReason`, logs `USER_MARKED_INVALID`.
- **Activate:** restores `Status = Active`, clears `InvalidReason`; logs distinct audit messages depending on whether the user was `Invalid` (`INVALID_REMOVED` vs `USER_ACTIVATED`).
- **Deactivate:** sets `Status = Inactive`, logs `USER_DEACTIVATED`. Both activate/deactivate refuse a user already in `Deleted` status.

### Soft Delete (`DeleteUserAsync`)
- Refuses the Super Admin and already-deleted users.
- **Soft delete** (no hard delete): renames `UserName` → `deleted_user_{id}`, sets `Email` → `deleted_{id}@deleted.local`, blanks `FirstName/LastName/Bio/AvatarUrl`, clears `EmailConfirmed`, sets `Status = Deleted`.
- Revokes all non-revoked `RefreshTokens` for the user so any active sessions die.
- Logs `USER_SOFT_DELETED`. Identity constraints require the unique username/email to change before reuse — hence the renames.

### Certificates (controller-level, direct EF + auditing)
- Paged/searchable listing (`page ≥ 1`, `pageSize` clamped 1–100). Search matches first name, last name, full name, email, or path title. Date filters (`fromDate`/`toDate`) parsed via `DateTime.TryParse` and applied to `IssuedAt`.
- Certificate number is derived, not stored: `$"CERT-{Id:D6}"` (zero-padded 6-digit). `CompletedAt` mirrors `IssuedAt`.
- Deletion removes the row and writes a `CERTIFICATE_DELETED` audit.

### Classrooms (controller-level, direct EF + auditing)
- Paged/searchable listing; search matches title, invite code, path title, creator name/email; optional filter by `learningPathId`; `status == "Archived"` intentionally matches nothing (no archived state today — filter reserved for future).
- `MemberCount = c.UserClassrooms.Count`; every classroom is reported `Status = "Active"`.
- Detail loads members (ordered by join date) with user status + invalid reason; classroom membership has no separate role.
- Update requires a title; can optionally reassign `LearningPathId` (validated to exist) and `TrainerId` (validated to exist). Changing `TrainerId` transfers `CreatedById` ownership and auto-joins the new manager.
- `ReassignLearningPath` refuses no-op reassignment, logs old → new path titles.
- Deletion removes the classroom and logs `CLASSROOM_DELETED` with path title. `JOINED/LOCKED` status of memory: classroom delete is cascading via EF relationships.

### Super Admin Model
`User.IsSuperAdmin` is a **boolean flag** separate from Identity roles. `AdminSeeder` keeps it true on the seed admin account across runs. The super-admin account cannot be marked invalid or deleted, and its roles are not editable. The audit-log viewer is Super-Admin-gated (feature 14).

---

## 6. Frontend Implementation

| Layer | File Path | Responsibility |
|---|---|---|
| Page | `frontend/src/pages/admin/AdminPage.tsx` | Layout/nav shell + stats dashboard |
| Page | `frontend/src/pages/admin/AdminUsersPage.tsx` | User list, search, read-only role display, status actions |
| Page | `frontend/src/pages/admin/AdminCertificatesPage.tsx` | Certificate list/detail/delete |
| Page | `frontend/src/pages/admin/AdminClassroomsPage.tsx` | Classroom list/detail/edit/reassign/delete |
| Page | `frontend/src/pages/admin/AdminPathsPage.tsx` | All learning paths |
| Page | `frontend/src/pages/admin/AdminHistoryPage.tsx` | Audit log viewer (Super Admin gated) |
| Pages | `frontend/src/pages/admin/AdminQuizManagementPage.tsx`, `AdminQuizEditorPage.tsx`, `AdminQuizAnalyticsPage.tsx`, `AdminQuestionBanksPage.tsx`, `AdminModuleEditorPage.tsx` | Quiz/path content administration |

---

## 7. Backend Implementation

| Layer | File Path | Responsibility |
|---|---|---|
| Controller | `backend/Controllers/AdminController.cs` | `api/v1/admin/*` endpoints (stats, users, certificates, classrooms) |
| Service | `backend/Services/Admin/AdminService.cs` | Stats, user management, read-only role display, soft delete, paths |
| Interface | `backend/Interfaces/Services/IAdminService.cs` | Service contract |
| DTO | `backend/DTOs/AdminDto.cs` | Admin response/request models |
| Seeder | `backend/Data/Seeders/RoleSeeder.cs` | Seed Admin account + super-admin flag maintenance |

---

## 8. Database Implementation

| Entity/Table | Purpose |
|---|---|
| `AspNetUsers` (+ `IsSuperAdmin`, `Status`, `InvalidReason`) | User lifecycle state; `Status` enum Active/Inactive/Invalid/Deleted |
| `RefreshTokens` | Last-login heuristic + session revocation on soft delete |
| `Certificates` | Certificate listing, search, delete |
| `Classrooms` / `UserClassrooms` | Classroom listing, detail, members |
| `LearningPaths` / `Progresses` / `QuizAttempts` | Stats + per-user counters |

Migrations of note: `20260731094731_AddUserStatusAndSuperAdmin.cs` (adds `Status`, renames/backfills `IsSuperAdmin`, seeds flag), `20260731101137_AddInvalidStatusAndQuizOwnership.cs` (adds invalid status + quiz ownership backfill to the Super Admin).

---

## 9. Security & Authorization

- Whole controller is `[Authorize(Roles = "Admin")]` — the endpoint surface is Admin-only.
- **Role display** is read-only; there is no role-update endpoint or service method.
- **Mark invalid** requires the `Admin` role; the seed super-admin account is protected.
- **Soft delete / activate / deactivate** — Admin only, with super-admin protection and deleted-user guards.
- `KeyNotFoundException` → 404; `ArgumentException` → 400; `UnauthorizedAccessException` → (403 via middleware) for guarded actions.
- All mutations emit audit log entries (see feature 14).

---

## 10. Important Business Rules

1. Roles are limited to `Admin` and `Student`; they are read-only in the UI and API.
2. The seed super-admin account cannot be deleted or marked invalid.
3. **Soft delete** preserves row integrity (rename to `deleted_user_{id}` + `deleted_{id}@deleted.local`) and revokes active refresh tokens.
4. `MarkInvalid` requires a reason; deleted users cannot be activated/deactivated/invalidated.
5. Certificate number is computed (`CERT-{Id:D6}`), not stored.
6. Classroom `Archived` status filter matches nothing today (reserved).
7. Last login is approximated by the max refresh-token issuance time.
8. Certificates list date range is inclusive (`IssuedAt >= from` and `IssuedAt <= to`).

---

## 11. Example of Internal Execution

### Step 1: Admin views a user's roles
- `GET /api/v1/admin/users` returns each user's `Admin`/`Student` roles for display.
- There is no `PUT /api/v1/admin/users/{id}/role` endpoint; role changes are not supported.

### Step 2: Admin marks a user invalid
- `PUT /api/v1/admin/users/u7/invalid` with `{ reason: "Plagiarism" }`.
- The actor has the `Admin` role → pass; the target is not the protected super-admin account → pass; status becomes `Invalid` and `USER_MARKED_INVALID` is logged.

### Step 3: Admin soft-deletes a user
- `DELETE /api/v1/admin/users/abc123`.
- User renamed to `deleted_user_abc123`, email → `deleted_abc123@deleted.local`, status `Deleted`, all refresh tokens revoked, `USER_SOFT_DELETED` audit written.

### Step 4: Stats dashboard
- `GET /api/v1/admin/stats` — 6-month growth: `[("Mar",4),("Apr",9),("May",12),("Jun",20),("Jul",31),("Aug",18)]` plus current totals.

---

## 12. Files Involved

### Frontend
- `frontend/src/pages/admin/AdminPage.tsx`
- `frontend/src/pages/admin/AdminUsersPage.tsx`
- `frontend/src/pages/admin/AdminCertificatesPage.tsx`
- `frontend/src/pages/admin/AdminClassroomsPage.tsx`
- `frontend/src/pages/admin/AdminPathsPage.tsx`
- `frontend/src/pages/admin/AdminHistoryPage.tsx`
- `frontend/src/pages/admin/AdminQuizManagementPage.tsx`, `AdminQuizEditorPage.tsx`, `AdminQuizAnalyticsPage.tsx`, `AdminQuestionBanksPage.tsx`, `AdminModuleEditorPage.tsx`

### Backend
- `backend/Controllers/AdminController.cs`
- `backend/Services/Admin/AdminService.cs`
- `backend/Interfaces/Services/IAdminService.cs`
- `backend/DTOs/AdminDto.cs`
- `backend/Data/Seeders/RoleSeeder.cs`

### Database
- `backend/Entities/User.cs` (`IsSuperAdmin`, `Status`)
- `backend/Migrations/20260731094731_AddUserStatusAndSuperAdmin.cs`
- `backend/Migrations/20260731101137_AddInvalidStatusAndQuizOwnership.cs`

---

## 13. Functionality

- Platform-wide stats with 6-month user-growth series
- User search/list/detail with live activity counters
- Read-only role display (Admin/Student)
- Mark-invalid with required reason (Admin; seed super-admin protected)
- Activate / deactivate users
- Soft delete with token revocation and identity renaming
- Certificate search/filter/paginate, detail, delete
- Classroom search/filter/paginate, detail, edit, reassign path, delete
- Learning-path list (all paths)
- Super Admin flag model + guarded endpoints
- Audit logging on every admin mutation