using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearnPath.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInstructorRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old membership role was the source of classroom facilitator access.
            // Preserve that access by transferring ownership only when the recorded
            // creator was not an Instructor member of the classroom.
            migrationBuilder.Sql("""
                UPDATE c
                SET c.CreatedById = x.UserId
                FROM Classrooms c
                CROSS APPLY (
                    SELECT TOP (1) uc.UserId
                    FROM UserClassrooms uc
                    WHERE uc.ClassroomId = c.Id AND uc.Role = N'Instructor'
                    ORDER BY uc.JoinedAt, uc.UserId
                ) x
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM UserClassrooms ownerMembership
                    WHERE ownerMembership.ClassroomId = c.Id
                      AND ownerMembership.UserId = c.CreatedById
                      AND ownerMembership.Role = N'Instructor'
                );
                """);

            // Every removed Instructor must retain a valid role. Former Instructor-only
            // users become Students; users who already have Admin or Student keep it.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'STUDENT')
                BEGIN
                    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                    VALUES (CONVERT(nvarchar(450), NEWID()), N'Student', N'STUDENT', CONVERT(nvarchar(450), NEWID()));
                END;

                DECLARE @studentRoleId nvarchar(450) =
                    (SELECT Id FROM AspNetRoles WHERE NormalizedName = N'STUDENT');

                INSERT INTO AspNetUserRoles (UserId, RoleId)
                SELECT affected.UserId, @studentRoleId
                FROM (
                    SELECT DISTINCT ur.UserId
                    FROM AspNetUserRoles ur
                    INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                    WHERE r.NormalizedName = N'INSTRUCTOR'
                ) affected
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM AspNetUserRoles existing
                    INNER JOIN AspNetRoles existingRole ON existingRole.Id = existing.RoleId
                    WHERE existing.UserId = affected.UserId
                      AND existingRole.NormalizedName IN (N'ADMIN', N'STUDENT')
                );
                """);

            // Revoke refresh tokens for affected non-Admin users so stale Instructor
            // claims cannot be renewed. Admin tokens are intentionally left intact.
            migrationBuilder.Sql("""
                UPDATE rt
                SET rt.IsRevoked = 1
                FROM RefreshTokens rt
                WHERE rt.IsRevoked = 0
                  AND EXISTS (
                      SELECT 1
                      FROM AspNetUserRoles ur
                      INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                      WHERE ur.UserId = rt.UserId AND r.NormalizedName = N'INSTRUCTOR'
                  )
                  AND NOT EXISTS (
                      SELECT 1
                      FROM AspNetUserRoles adminLink
                      INNER JOIN AspNetRoles adminRole ON adminRole.Id = adminLink.RoleId
                      WHERE adminLink.UserId = rt.UserId AND adminRole.NormalizedName = N'ADMIN'
                  );
                """);

            // Remove the role and any role/user claims that still refer to it.
            migrationBuilder.Sql("""
                DELETE rc
                FROM AspNetRoleClaims rc
                INNER JOIN AspNetRoles r ON r.Id = rc.RoleId
                WHERE r.NormalizedName = N'INSTRUCTOR';

                DELETE ur
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                WHERE r.NormalizedName = N'INSTRUCTOR';

                DELETE FROM AspNetUserClaims
                WHERE ClaimValue = N'Instructor'
                  AND ClaimType IN (
                      N'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
                      N'role'
                  );

                DELETE FROM AspNetRoles WHERE NormalizedName = N'INSTRUCTOR';
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "UserClassrooms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Schema-only rollback. The Up migration intentionally cannot restore
            // Instructor role links, revoked refresh tokens, or membership roles.
            // A database backup is required for a full rollback.
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "UserClassrooms",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Student");
        }
    }
}
