# Instructor Role Removal — Migration Record

## Summary

LearnPath now has exactly two application roles: `Admin` and `Student`. The retired
`Instructor` role and classroom membership role have been removed from active code,
authorization, registration, UI, seed data, and tests.

- Migration: `20261023085336_RemoveInstructorRole`
- Applied to the live `LearnPathDb` on 2026-10-23
- Pre-migration backup: `C:\Users\Shadrul\AppData\Local\Temp\opencode\LearnPathDb_pre_role_removal_20261023.bak`
- Backup verification: restored successfully to a scratch database and inspected before applying the migration.

The backup is intentionally stored outside the repository. It must not be committed.

## Irreversible transformations

The migration performs the following data changes before dropping
`UserClassrooms.Role`:

1. **Role links and claims**
   - All `AspNetUserRoles` links to the `Instructor` role are deleted.
   - Role/user claims that reference `Instructor` are deleted.
   - The `Instructor` row is deleted from `AspNetRoles`.
   - Former Instructor-only users are assigned `Student`. Users who already hold
     `Admin` or `Student` keep their existing role.

2. **Refresh tokens**
   - Active refresh tokens for affected non-Admin users are revoked. Admin refresh
     tokens are left intact, so the Admin account is not invalidated.
   - Access tokens already issued remain valid until expiry, but an Instructor role
     claim no longer grants any endpoint access because the role and policy are gone.

3. **Classroom management**
   - `UserClassrooms.Role` is dropped.
   - Classroom management is now represented only by `Classroom.CreatedById`.
   - If a classroom's recorded creator was not an Instructor member, but the
     classroom had an Instructor member, ownership transfers to the earliest-joined
     Instructor member. That member keeps management access.
   - Other Instructor memberships become ordinary Student memberships and lose
     classroom management access.
   - Admin reassignment of `CreatedById` is an ownership transfer and auto-joins the
     new manager.

4. **Audit history**
   - Existing audit rows are not rewritten. Historical rows may still contain
     `Instructor` as text.
   - `AuditAction.INSTRUCTOR_ASSIGNED = 13` and `AuditAction.INSTRUCTOR_REMOVED = 14`
     remain only to preserve the meaning of historical rows. No active code path
     emits them, and they are excluded from new audit-filter options.

## Rollback limits

The EF Core `Down` migration only re-adds the `UserClassrooms.Role` column with a
default value. It cannot restore:

- deleted Instructor role links or claims,
- revoked refresh tokens,
- former Instructor memberships or management access, or
- the deleted `Instructor` role row.

A full rollback requires restoring the pre-migration backup listed above. Do not run
`Down` against production and assume the former role assignments are restored.

## Post-migration verification

The live database was checked after migration:

- 7 users, 2 learning paths, 14 modules, 1 classroom, 2 memberships, 1 progress row,
  and 172 refresh-token rows preserved.
- Only `Admin` and `Student` roles remain.
- No Instructor role links or `UserClassrooms.Role` column remain.
- Admin login and classroom management still work.
- Students receive `403` for authoring/admin endpoints.
- The removed role-update route returns `404`.
