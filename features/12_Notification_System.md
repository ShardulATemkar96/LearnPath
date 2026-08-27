# Notification System

## 1. Feature Name

Notification System

## 2. Purpose

- **Problem Solved:** Users need a centralized inbox for system messages (module completions, classroom invites, quiz results, community replies).
- **Why It Exists:** Provides a pull-based notification model (polled via API, displayed in a header dropdown) with read/unread tracking and a mark-all-read action.
- **Role in Application:** It is a cross-cutting notification layer that any service should be able to write to. Currently the infrastructure (entity, service, controller, frontend) is complete but **no producer has been wired up** — the `CreateAsync` method is never called from any other service.

---

## 3. What the User Can Do

- View a list of their notifications (title, message, type, timestamp, read status), ordered newest-first.
- Mark a single notification as read (click-to-dismiss visual unread state).
- Mark all notifications as read in one action.
- See an unread count badge in the header.
- (Gap) Notifications are never created — no part of the system calls `NotificationService.CreateAsync`.

---

## 4. Feature Workflow

```
[System event occurs (e.g., module completed, quiz graded)]
  → (would call) INotificationService.CreateAsync(userId, title, message, type)
  → Notification row inserted (IsRead = false)
  → [User opens app] → GET /api/v1/notifications → dropdown shows unread count
  → [User clicks notification] → PUT /api/v1/notifications/{id}/read
  → [User clicks "Mark all read"] → PUT /api/v1/notifications/read-all
```

---

## 5. How It Works Internally

### Notification Entity
`Notification` (columns: `Id`, `UserId`, `Title`, `Message`, `Type`, `IsRead`, `CreatedAt`). `IsRead` defaults to `false`, `CreatedAt` defaults to `UtcNow`.

### Service Methods
- **`GetMyNotificationsAsync(userId)`** — `OrderByDescending(CreatedAt)`, maps to `NotificationResponseDto`.
- **`MarkReadAsync(id, userId)`** — loads by `Id + UserId` (ownership check); throws `KeyNotFoundException` if not found; sets `IsRead = true`.
- **`MarkAllReadAsync(userId)`** — loads all unread notifications for the user; sets `IsRead = true` on each.
- **`CreateAsync(userId, title, message, type)`** — inserts a new row. **Not called anywhere in the codebase.**

### No Producers
A grep for `INotificationService.CreateAsync` or `_context.Notifications.Add` outside of `NotificationService` returns zero results. No other service (Auth, Classroom, Quiz, Community, Assignment, Progress) currently emits notifications. This is an unimplemented gap: the notification pipeline has no source events.

---

## 6. Frontend Implementation

| Layer | File Path | Responsibility |
|---|---|---|
| Component | `frontend/src/components/dashboard/Topbar/Topbar.tsx` | Notification dropdown (bell icon, unread count badge, top-10 list, mark-all-read button) |
| State | `frontend/src/redux/slices/notificationSlice.ts` | Redux slice with `fetchNotificationsThunk`, `markReadThunk`, `markAllReadThunk` |
| Service | `frontend/src/services/notificationService.ts` | `getMy()`, `markRead(id)`, `markAllRead()` |

The Topbar dropdown:
- Displays up to 10 notifications; unread items have a colored left-border (type-based color), bold title, and blue-tinted background.
- Clicking an unread item dispatches `markReadThunk`.
- The "Mark all read" button dispatches `markAllReadThunk`.
- An empty state shows "No notifications yet."

---

## 7. Backend Implementation

| Layer | File Path | Responsibility |
|---|---|---|
| Controller | `backend/Controllers/NotificationController.cs` | `GET /api/v1/notifications`, `PUT /{id}/read`, `PUT /read-all` |
| Service | `backend/Services/Notification/NotificationService.cs` | CRUD for notifications, ownership guard |
| Interface | `backend/Interfaces/Services/INotificationService.cs` | Service contract |

---

## 8. Database Implementation

| Entity/Table | Purpose |
|---|---|
| `Notifications` | Per-user notification rows with title, message, type, read status, timestamp |

- Migration: included in `ApplicationDbContext` via `DbSet<Notification>`.

---

## 9. Security & Authorization

- All endpoints require `[Authorize]`; `userId` is extracted from the JWT `NameIdentifier` claim.
- `MarkReadAsync` enforces ownership: the notification must belong to the requesting user.
- No role-based access — all authenticated users can manage their own notifications.
- No admin endpoint for sending broadcast notifications exists.

---

## 10. Important Business Rules

1. **No producers exist** — `CreateAsync` is defined but never called. The feature is structurally complete but functionally inert.
2. Notification ordering is newest-first (no pagination — all notifications returned in one list).
3. Notifications are per-user; there is no broadcast/group notification mechanism.
4. The `Type` string is free-form (no enum); the frontend maps it to a color (`TYPE_COLOR`).
5. `MarkAllReadAsync` avoids a round-trip per row by loading all unread and bulk-setting `IsRead`.

---

## 11. Example of Internal Execution

### Hypothetical scenario (not yet wired)
1. A user completes a module.
2. `ProgressService` (or an event handler) would call `INotificationService.CreateAsync(userId, "Module Completed", "You completed 'Variables 101'", "completion")`.
3. The user opens the app; the Topbar polls `GET /api/v1/notifications` → the notification appears with a green left-border.
4. The user clicks it → `PUT /notifications/5/read` → `IsRead` flips to `true` → the badge count decrements.

### Current reality
- The notification list is always empty.
- Frontend displays "No notifications yet."

---

## 12. Files Involved

### Frontend
- `frontend/src/components/dashboard/Topbar/Topbar.tsx`
- `frontend/src/redux/slices/notificationSlice.ts`
- `frontend/src/services/notificationService.ts`

### Backend
- `backend/Controllers/NotificationController.cs`
- `backend/Services/Notification/NotificationService.cs`
- `backend/Interfaces/Services/INotificationService.cs`

### Database
- `backend/Entities/Notification.cs`

---

## 13. Functionality

- Notification list retrieval (newest-first)
- Single notification mark-as-read (ownership-guarded)
- Bulk mark-all-as-read
- Frontend notification dropdown with unread badge, type-colored borders, and empty state
- Create infrastructure (method defined, ready to wire) **GAP: no producers implemented**