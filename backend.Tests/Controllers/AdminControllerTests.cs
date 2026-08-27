using FluentAssertions;
using LearnPath.API.Common;
using LearnPath.API.Controllers;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Certificate;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.Interfaces.Services;
using LearnPath.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LearnPath.Tests.Controllers;

public class AdminControllerTests
{
    private static (ApplicationDbContext ctx, AdminController controller) CreateController()
    {
        var ctx = DbContextFactory.Create();
        var controller = new AdminController(
            Mock.Of<IAdminService>(),
            ctx,
            new FakeAuditLogService());
        return (ctx, controller);
    }

    private static async Task SeedCertificatesAsync(
        ApplicationDbContext ctx,
        string ownerId,
        string studentId)
    {
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId, email: "owner@learnpath.dev", firstName: "Ada", lastName: "Lovelace"));
        ctx.Users.Add(EntityFactory.CreateUser(id: studentId, email: "student@learnpath.dev", firstName: "Grace", lastName: "Hopper"));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId, title: "Intro to Java"));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 2, createdById: ownerId, title: "Advanced C#"));
        ctx.Certificates.Add(new LearnPath.API.Entities.Certificate
        {
            Id = 1,
            UserId = studentId,
            LearningPathId = 1,
            CertificateUrl = "/certificates/g/h1",
            IssuedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
        });
        ctx.Certificates.Add(new LearnPath.API.Entities.Certificate
        {
            Id = 2,
            UserId = studentId,
            LearningPathId = 2,
            CertificateUrl = "/certificates/g/h2",
            IssuedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc),
        });
        await ctx.SaveChangesAsync();
    }

    /* ── GET /admin/certificates ─────────────────────────── */

    [Fact]
    public async Task GetCertificates_ReturnsAllCertificatesMapped()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.GetCertificates(null, null, null, 1, 20);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<AdminCertificateListResponseDto>)ok.Value!;
        body.Success.Should().BeTrue();
        body.Data!.TotalCount.Should().Be(2);
        body.Data.Entries.Should().HaveCount(2);
        body.Data.Entries.Should().OnlyContain(e => e.Status == "Issued");
        body.Data.Entries.Should().Contain(e => e.UserName == "Grace Hopper" && e.LearningPathTitle == "Advanced C#");
        body.Data.Entries[0].CertificateNumber.Should().Be("CERT-000002");
    }

    [Fact]
    public async Task GetCertificates_SearchFiltersByNameEmailAndPath()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var byName = await controller.GetCertificates("Grace", null, null, 1, 20);
        var byNameBody = (ApiResponse<AdminCertificateListResponseDto>)((OkObjectResult)byName).Value!;
        byNameBody.Data!.TotalCount.Should().Be(2);

        var byEmail = await controller.GetCertificates("student@learnpath.dev", null, null, 1, 20);
        var byEmailBody = (ApiResponse<AdminCertificateListResponseDto>)((OkObjectResult)byEmail).Value!;
        byEmailBody.Data!.TotalCount.Should().Be(2);

        var byPath = await controller.GetCertificates("C#", null, null, 1, 20);
        var byPathBody = (ApiResponse<AdminCertificateListResponseDto>)((OkObjectResult)byPath).Value!;
        byPathBody.Data!.TotalCount.Should().Be(1);
        byPathBody.Data!.Entries[0].LearningPathTitle.Should().Be("Advanced C#");

        var noMatch = await controller.GetCertificates("zzz", null, null, 1, 20);
        var noMatchBody = (ApiResponse<AdminCertificateListResponseDto>)((OkObjectResult)noMatch).Value!;
        noMatchBody.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCertificates_FiltersByDateRange()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.GetCertificates(null, "2026-01-15", "2026-02-28", 1, 20);

        var body = (ApiResponse<AdminCertificateListResponseDto>)((OkObjectResult)result).Value!;
        body.Data!.TotalCount.Should().Be(1);
        body.Data.Entries[0].LearningPathTitle.Should().Be("Advanced C#");
    }

    [Fact]
    public async Task GetCertificates_ClampsPageAndPageSize()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.GetCertificates(null, null, null, 0, 500);

        var body = (ApiResponse<AdminCertificateListResponseDto>)((OkObjectResult)result).Value!;
        body.Data!.Page.Should().Be(1);
        body.Data.PageSize.Should().Be(100);
    }

    /* ── GET /admin/certificates/{id} ────────────────────── */

    [Fact]
    public async Task GetCertificate_ReturnsMappedDto()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.GetCertificate(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<AdminCertificateResponseDto>)ok.Value!;
        body.Data!.CertificateNumber.Should().Be("CERT-000001");
        body.Data.UserEmail.Should().Be("student@learnpath.dev");
        body.Data.LearningPathTitle.Should().Be("Intro to Java");
        body.Data.CertificateUrl.Should().Be("/certificates/g/h1");
    }

    [Fact]
    public async Task GetCertificate_NotFound_ThrowsKeyNotFound()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var act = async () => await controller.GetCertificate(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /* ── DELETE /admin/certificates/{id} ─────────────────── */

    [Fact]
    public async Task DeleteCertificate_RemovesCertificate()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.DeleteCertificate(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<object>)ok.Value!;
        body.Success.Should().BeTrue();
        (await ctx.Certificates.CountAsync()).Should().Be(1);
        (await ctx.Certificates.AnyAsync(c => c.Id == 1)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCertificate_NotFound_ThrowsKeyNotFound()
    {
        var (ctx, controller) = CreateController();
        await SeedCertificatesAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var act = async () => await controller.DeleteCertificate(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Certificates.CountAsync()).Should().Be(2);
    }

    /* ── Classrooms ─────────────────────────────────────── */

    private static async Task SeedClassroomsAsync(
        ApplicationDbContext ctx,
        string ownerId,
        string memberId)
    {
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId, email: "trainer@learnpath.dev", firstName: "Ada", lastName: "Lovelace"));
        ctx.Users.Add(EntityFactory.CreateUser(id: memberId, email: "member@learnpath.dev", firstName: "Grace", lastName: "Hopper"));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId, title: "Intro to Java"));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 2, createdById: ownerId, title: "Advanced C#"));

        ctx.Classrooms.Add(EntityFactory.CreateClassroom(
            id: 1, createdById: ownerId, learningPathId: 1));
        ctx.Classrooms.Add(EntityFactory.CreateClassroom(
            id: 2, createdById: ownerId, learningPathId: 2));

        ctx.UserClassrooms.Add(new LearnPath.API.Entities.UserClassroom
        {
            UserId = ownerId,
            ClassroomId = 1,
            Role = "Instructor",
            JoinedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
        });
        ctx.UserClassrooms.Add(new LearnPath.API.Entities.UserClassroom
        {
            UserId = memberId,
            ClassroomId = 1,
            Role = "Student",
            JoinedAt = new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc),
        });

        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task GetClassrooms_ReturnsAllMappedWithCounts()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.GetClassrooms(null, null, null, 1, 20);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<AdminClassroomListResponseDto>)ok.Value!;
        body.Success.Should().BeTrue();
        body.Data!.TotalCount.Should().Be(2);
        body.Data.Entries.Should().HaveCount(2);
        body.Data.Entries.Should().OnlyContain(e => e.Status == "Active");
        body.Data.Entries.Should().Contain(e => e.Title == "Test Classroom"
            && e.InviteCode == "TESTCODE"
            && e.MemberCount == 2
            && e.CreatedByName == "Ada Lovelace"
            && e.LearningPathTitle == "Intro to Java");
    }

    [Fact]
    public async Task GetClassrooms_SearchFiltersByNameCodePathAndTrainer()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var byTrainer = await controller.GetClassrooms("Ada", null, null, 1, 20);
        ((ApiResponse<AdminClassroomListResponseDto>)((OkObjectResult)byTrainer).Value!)
            .Data!.TotalCount.Should().Be(2);

        var byPath = await controller.GetClassrooms("C#", null, null, 1, 20);
        ((ApiResponse<AdminClassroomListResponseDto>)((OkObjectResult)byPath).Value!)
            .Data!.TotalCount.Should().Be(1);

        var byCode = await controller.GetClassrooms("TESTCODE", null, null, 1, 20);
        ((ApiResponse<AdminClassroomListResponseDto>)((OkObjectResult)byCode).Value!)
            .Data!.TotalCount.Should().Be(2);

        var noMatch = await controller.GetClassrooms("zzz", null, null, 1, 20);
        ((ApiResponse<AdminClassroomListResponseDto>)((OkObjectResult)noMatch).Value!)
            .Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetClassrooms_FiltersByLearningPathAndPagination()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var byPath = await controller.GetClassrooms(null, 2, null, 1, 20);
        var byPathBody = (ApiResponse<AdminClassroomListResponseDto>)((OkObjectResult)byPath).Value!;
        byPathBody.Data!.TotalCount.Should().Be(1);
        byPathBody.Data.Entries[0].LearningPathTitle.Should().Be("Advanced C#");

        var page2 = await controller.GetClassrooms(null, null, null, 2, 1);
        var page2Body = (ApiResponse<AdminClassroomListResponseDto>)((OkObjectResult)page2).Value!;
        page2Body.Data!.Page.Should().Be(2);
        page2Body.Data!.Entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetClassrooms_ArchivedStatusFilterReturnsNone()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.GetClassrooms(null, null, "Archived", 1, 20);

        var body = (ApiResponse<AdminClassroomListResponseDto>)((OkObjectResult)result).Value!;
        body.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetClassroom_ReturnsDetailWithMembers()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.GetClassroom(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<AdminClassroomDetailResponseDto>)ok.Value!;
        body.Data!.MemberCount.Should().Be(2);
        body.Data.AssignmentCount.Should().Be(0);
        body.Data.Members.Should().HaveCount(2);
        body.Data.Members.Should().Contain(m => m.Role == "Instructor" && m.FullName == "Ada Lovelace");
        body.Data.Members.Should().Contain(m => m.Role == "Student" && m.FullName == "Grace Hopper");
    }

    [Fact]
    public async Task GetClassroom_NotFound_ThrowsKeyNotFound()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var act = async () => await controller.GetClassroom(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateClassroom_UpdatesFieldsAndReassignsPath()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.UpdateClassroom(1, new UpdateAdminClassroomDto
        {
            Title = "Renamed Classroom",
            Description = "Updated description",
            LearningPathId = 2,
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<AdminClassroomResponseDto>)ok.Value!;
        body.Data!.Title.Should().Be("Renamed Classroom");
        body.Data.Description.Should().Be("Updated description");
        body.Data.LearningPathTitle.Should().Be("Advanced C#");
        body.Data.MemberCount.Should().Be(2);

        var stored = await ctx.Classrooms.FirstAsync(c => c.Id == 1);
        stored.Title.Should().Be("Renamed Classroom");
        stored.LearningPathId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateClassroom_BlankTitle_ThrowsArgumentException()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var act = async () => await controller.UpdateClassroom(1, new UpdateAdminClassroomDto
        {
            Title = "   ",
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateClassroom_ReassignsTrainer()
    {
        var (ctx, controller) = CreateController();
        var ownerId = Guid.NewGuid().ToString();
        var memberId = Guid.NewGuid().ToString();
        await SeedClassroomsAsync(ctx, ownerId, memberId);

        var result = await controller.UpdateClassroom(1, new UpdateAdminClassroomDto
        {
            Title = "Test Classroom",
            TrainerId = memberId,
        });

        var body = (ApiResponse<AdminClassroomResponseDto>)((OkObjectResult)result).Value!;
        body.Data!.CreatedById.Should().Be(memberId);
    }

    [Fact]
    public async Task ReassignLearningPath_ReassignsAndAudits()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.ReassignLearningPath(1, new ReassignClassroomLearningPathDto
        {
            LearningPathId = 2,
        });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<AdminClassroomResponseDto>)ok.Value!;
        body.Data!.LearningPathTitle.Should().Be("Advanced C#");
        (await ctx.Classrooms.FirstAsync(c => c.Id == 1)).LearningPathId.Should().Be(2);
    }

    [Fact]
    public async Task ReassignLearningPath_SamePath_ThrowsArgumentException()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var act = async () => await controller.ReassignLearningPath(1, new ReassignClassroomLearningPathDto
        {
            LearningPathId = 1,
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReassignLearningPath_MissingPath_ThrowsKeyNotFound()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var act = async () => await controller.ReassignLearningPath(1, new ReassignClassroomLearningPathDto
        {
            LearningPathId = 999,
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteClassroom_RemovesClassroomAndCascadesMemberships()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var result = await controller.DeleteClassroom(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = (ApiResponse<object>)ok.Value!;
        body.Success.Should().BeTrue();
        (await ctx.Classrooms.CountAsync()).Should().Be(1);
        (await ctx.UserClassrooms.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteClassroom_NotFound_ThrowsKeyNotFound()
    {
        var (ctx, controller) = CreateController();
        await SeedClassroomsAsync(ctx, Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        var act = async () => await controller.DeleteClassroom(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Classrooms.CountAsync()).Should().Be(2);
    }
}
