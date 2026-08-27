using FluentAssertions;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Community;
using LearnPath.API.Entities;
using LearnPath.API.Services.Community;
using LearnPath.API.Repositories;
using LearnPath.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnPath.Tests.Services;

public class CommunityServiceTests
{
    [Fact]
    public async Task GetPostsAsync_NoPosts_ReturnsEmpty()
    {
        // Arrange
        var context = DbContextFactory.Create();
        var service = new CommunityService(context, new FakeAuditLogService());

        // Act
        var result = await service.GetPostsAsync(
            null, null, null, PostSortOrder.Newest, PostFilter.All, 1, 10, "any-user");

        // Assert
        result.Posts.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetPostsAsync_WithPosts_ReturnsPaginatedCorrectly()
    {
        // Arrange
        var context  = DbContextFactory.Create();
        var authorId = Guid.NewGuid().ToString();
        var author   = EntityFactory.CreateUser(id: authorId);
        await context.Users.AddAsync(author);

        for (int i = 1; i <= 15; i++)
        {
            var post = EntityFactory.CreatePost(
                id: i, authorId: authorId,
                title: $"Post {i}");
            await context.Posts.AddAsync(post);
        }
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        // Act
        var result = await service.GetPostsAsync(
            null, null, null, PostSortOrder.Newest, PostFilter.All, page: 1, pageSize: 10, userId: authorId);

        // Assert
        result.Posts.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetPostsAsync_WithCategoryFilter_ReturnsFiltered()
    {
        // Arrange
        var context  = DbContextFactory.Create();
        var authorId = Guid.NewGuid().ToString();
        var author   = EntityFactory.CreateUser(id: authorId);
        await context.Users.AddAsync(author);

        await context.Posts.AddRangeAsync(
            EntityFactory.CreatePost(1, authorId, category: "General"),
            EntityFactory.CreatePost(2, authorId, category: "Questions"),
            EntityFactory.CreatePost(3, authorId, category: "Questions")
        );
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        // Act
        var result = await service.GetPostsAsync(
            "Questions", null, null, PostSortOrder.Newest, PostFilter.All, 1, 10, authorId);

        // Assert
        result.Posts.Should().HaveCount(2);
        result.Posts.Should().AllSatisfy(
            p => p.Category.Should().Be("Questions"));
    }

    [Fact]
    public async Task CreatePostAsync_ValidDto_CreatesPost()
    {
        // Arrange
        var context  = DbContextFactory.Create();
        var authorId = Guid.NewGuid().ToString();
        var author   = EntityFactory.CreateUser(id: authorId);
        await context.Users.AddAsync(author);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());
        var dto     = new CreatePostDto
        {
            Title    = "My Test Post",
            Content  = "This is the content of my test post.",
            Category = "General",
        };

        // Act
        var result = await service.CreatePostAsync(dto, authorId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("My Test Post");
        result.AuthorId.Should().Be(authorId);

        var count = await context.Posts.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task DeletePostAsync_OtherUserPost_ThrowsUnauthorized()
    {
        // Arrange
        var context  = DbContextFactory.Create();
        var authorId = Guid.NewGuid().ToString();
        var otherId  = Guid.NewGuid().ToString();
        var author   = EntityFactory.CreateUser(id: authorId);
        var other    = EntityFactory.CreateUser(id: otherId, email: "o@t.com");
        await context.Users.AddRangeAsync(author, other);

        var post = EntityFactory.CreatePost(id: 1, authorId: authorId);
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DeletePostAsync(1, otherId));
    }

    [Fact]
    public async Task VotePostAsync_FirstUpvote_ReturnsOne()
    {
        // Arrange
        var context  = DbContextFactory.Create();
        var authorId = Guid.NewGuid().ToString();
        var voterId  = Guid.NewGuid().ToString();
        var author   = EntityFactory.CreateUser(id: authorId);
        var voter    = EntityFactory.CreateUser(id: voterId, email: "v@t.com");
        await context.Users.AddRangeAsync(author, voter);

        var post = EntityFactory.CreatePost(id: 1, authorId: authorId);
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        // Act
        var count = await service.VotePostAsync(
            1, new VoteDto { IsUpvote = true }, voterId);

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task VotePostAsync_SameVoteTwice_TogglesOff()
    {
        // Arrange
        var context  = DbContextFactory.Create();
        var authorId = Guid.NewGuid().ToString();
        var voterId  = Guid.NewGuid().ToString();
        var author   = EntityFactory.CreateUser(id: authorId);
        var voter    = EntityFactory.CreateUser(id: voterId, email: "v@t.com");
        await context.Users.AddRangeAsync(author, voter);

        var post = EntityFactory.CreatePost(id: 1, authorId: authorId);
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        await service.VotePostAsync(
            1, new VoteDto { IsUpvote = true }, voterId);

        var count = await service.VotePostAsync(
            1, new VoteDto { IsUpvote = true }, voterId);

        count.Should().Be(0);
    }

    [Fact]
    public async Task AddCommentAsync_LockedPost_ThrowsArgumentException()
    {
        // Arrange
        var context  = DbContextFactory.Create();
        var authorId = Guid.NewGuid().ToString();
        var author   = EntityFactory.CreateUser(id: authorId);
        await context.Users.AddAsync(author);

        var post = EntityFactory.CreatePost(id: 1, authorId: authorId);
        post.IsLocked = true;
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddCommentAsync(
                1, new CreateCommentDto { Content = "Hello" }, authorId));
        ex.Message.Should().Contain("locked");
    }

    // ── Groups ────────────────────────────────────────────────

    [Fact]
    public async Task JoinGroupAsync_PrivateGroup_ThrowsArgumentException()
    {
        var context = DbContextFactory.Create();
        var owner   = EntityFactory.CreateUser();
        var user    = EntityFactory.CreateUser(email: "u@t.com");
        await context.Users.AddRangeAsync(owner, user);

        var group = new Group
        {
            Id          = 1,
            Name        = "Private",
            Description = "d",
            OwnerId     = owner.Id,
            IsPublic    = false,
        };
        await context.Groups.AddAsync(group);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.JoinGroupAsync(1, user.Id));
        ex.Message.Should().Contain("private");
    }

    [Fact]
    public async Task LeaveGroupAsync_Owner_ThrowsArgumentException()
    {
        var context = DbContextFactory.Create();
        var owner   = EntityFactory.CreateUser();
        await context.Users.AddAsync(owner);

        var group = new Group
        {
            Id          = 1,
            Name        = "G",
            Description = "d",
            OwnerId     = owner.Id,
            IsPublic    = true,
        };
        await context.Groups.AddAsync(group);
        await context.GroupMembers.AddAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId  = owner.Id,
            Role    = "Owner",
        });
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.LeaveGroupAsync(1, owner.Id));
        ex.Message.Should().Contain("owner");
    }

    // ── Group posts / membership gating ───────────────────────

    [Fact]
    public async Task CreateGroupPostAsync_NotMember_ThrowsUnauthorized()
    {
        var context = DbContextFactory.Create();
        var owner   = EntityFactory.CreateUser();
        var outsider = EntityFactory.CreateUser(email: "o@t.com");
        await context.Users.AddRangeAsync(owner, outsider);

        var group = new Group
        {
            Id          = 1,
            Name        = "G",
            Description = "d",
            OwnerId     = owner.Id,
            IsPublic    = true,
        };
        await context.Groups.AddAsync(group);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateGroupPostAsync(
                1, new CreatePostDto { Title = "T", Content = "C" }, outsider.Id));
    }

    [Fact]
    public async Task CreateGroupPostAsync_Member_CreatesPostInGroup()
    {
        var context = DbContextFactory.Create();
        var owner   = EntityFactory.CreateUser();
        await context.Users.AddAsync(owner);

        var group = new Group
        {
            Id          = 1,
            Name        = "G",
            Description = "d",
            OwnerId     = owner.Id,
            IsPublic    = true,
        };
        await context.Groups.AddAsync(group);
        await context.GroupMembers.AddAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId  = owner.Id,
            Role    = "Owner",
        });
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        var result = await service.CreateGroupPostAsync(
            1, new CreatePostDto { Title = "T", Content = "C" }, owner.Id);

        result.Should().NotBeNull();
        var post = await context.Posts.SingleAsync();
        post.GroupId.Should().Be(1);
        post.AuthorId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task VotePostAsync_DownvoteInGroup_MaintainsScore()
    {
        var context = DbContextFactory.Create();
        var owner   = EntityFactory.CreateUser();
        var member  = EntityFactory.CreateUser(email: "m@t.com");
        await context.Users.AddRangeAsync(owner, member);

        var group = new Group
        {
            Id          = 1,
            Name        = "G",
            Description = "d",
            OwnerId     = owner.Id,
            IsPublic    = true,
        };
        await context.Groups.AddAsync(group);
        await context.GroupMembers.AddAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId  = member.Id,
            Role    = "Member",
        });

        var post = EntityFactory.CreatePost(id: 1, authorId: owner.Id);
        post.GroupId = 1;
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        await service.VotePostAsync(1, new VoteDto { IsUpvote = false }, member.Id);

        (await context.Posts.SingleAsync()).Score.Should().Be(-1);
    }

    // ── Pinning ───────────────────────────────────────────────

    [Fact]
    public async Task PinPostAsync_SecondPin_ReplacesFirst()
    {
        var context = DbContextFactory.Create();
        var owner   = EntityFactory.CreateUser();
        var admin   = EntityFactory.CreateUser(email: "a@t.com");
        await context.Users.AddRangeAsync(owner, admin);

        var group = new Group
        {
            Id          = 1,
            Name        = "G",
            Description = "d",
            OwnerId     = owner.Id,
            IsPublic    = true,
        };
        await context.Groups.AddAsync(group);

        var first  = EntityFactory.CreatePost(id: 1, authorId: owner.Id, title: "First");
        var second = EntityFactory.CreatePost(id: 2, authorId: owner.Id, title: "Second");
        first.GroupId = second.GroupId = 1;
        await context.Posts.AddRangeAsync(first, second);

        await MakeAdminAsync(context, admin);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        await service.PinPostAsync(1, admin.Id);
        await service.PinPostAsync(2, admin.Id);

        var pinnedFirst  = await context.Posts.FindAsync(1);
        var pinnedSecond = await context.Posts.FindAsync(2);
        pinnedFirst.Should().NotBeNull();
        pinnedSecond.Should().NotBeNull();
        pinnedFirst!.IsPinned.Should().BeFalse();
        pinnedSecond!.IsPinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinPostAsync_NonGroupPost_ThrowsArgumentException()
    {
        var context = DbContextFactory.Create();
        var author  = EntityFactory.CreateUser();
        var admin   = EntityFactory.CreateUser(email: "a@t.com");
        await context.Users.AddRangeAsync(author, admin);

        var post = EntityFactory.CreatePost(id: 1, authorId: author.Id);
        await context.Posts.AddAsync(post);
        await MakeAdminAsync(context, admin);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.PinPostAsync(1, admin.Id));
        ex.Message.Should().Contain("group");
    }

    // ── Bans ──────────────────────────────────────────────────

    [Fact]
    public async Task BanUserFromGroupAsync_BannedMemberCannotPost()
    {
        var context = DbContextFactory.Create();
        var owner   = EntityFactory.CreateUser();
        var member  = EntityFactory.CreateUser(email: "m@t.com");
        await context.Users.AddRangeAsync(owner, member);

        var group = new Group
        {
            Id          = 1,
            Name        = "G",
            Description = "d",
            OwnerId     = owner.Id,
            IsPublic    = true,
        };
        await context.Groups.AddAsync(group);
        await context.GroupMembers.AddRangeAsync(
            new GroupMember { GroupId = group.Id, UserId = owner.Id, Role = "Owner" },
            new GroupMember { GroupId = group.Id, UserId = member.Id, Role = "Member" });
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        await service.BanUserFromGroupAsync(1, member.Id, owner.Id);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateGroupPostAsync(
                1, new CreatePostDto { Title = "T", Content = "C" }, member.Id));
        ex.Message.Should().Contain("banned");
    }

    // ── Editing / reports / trending ──────────────────────────

    [Fact]
    public async Task UpdatePostAsync_SetsEditedFlag()
    {
        var context = DbContextFactory.Create();
        var author  = EntityFactory.CreateUser();
        await context.Users.AddAsync(author);

        var post = EntityFactory.CreatePost(id: 1, authorId: author.Id);
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        await service.UpdatePostAsync(
            1, new UpdatePostDto { Title = "Edited", Content = "New", Category = "General" }, author.Id);

        var updated = await context.Posts.SingleAsync();
        updated.Edited.Should().BeTrue();
        updated.EditedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReportPostAsync_CreatesPendingReport()
    {
        var context = DbContextFactory.Create();
        var author  = EntityFactory.CreateUser();
        var reporter = EntityFactory.CreateUser(email: "r@t.com");
        await context.Users.AddRangeAsync(author, reporter);

        var post = EntityFactory.CreatePost(id: 1, authorId: author.Id);
        await context.Posts.AddAsync(post);
        await context.SaveChangesAsync();

        var service = new CommunityService(context, new FakeAuditLogService());

        await service.ReportPostAsync(1, new CreateReportDto { Reason = "Spam" }, reporter.Id);

        var report = await context.Reports.SingleAsync();
        report.TargetType.Should().Be("Post");
        report.TargetId.Should().Be(1);
        report.Status.Should().Be("Pending");
        report.ReportedByUserId.Should().Be(reporter.Id);
    }

    [Fact]
    public void CalculateTrendingScore_NewerPost_RanksHigher()
    {
        var now = DateTime.UtcNow;
        var fresh = CommunityService.CalculateTrendingScore(5, now);
        var stale = CommunityService.CalculateTrendingScore(5, now.AddDays(-30));

        fresh.Should().BeGreaterThan(stale);
    }

    private static async Task MakeAdminAsync(ApplicationDbContext context, User user)
    {
        var role = new IdentityRole("Admin") { Id = Guid.NewGuid().ToString() };
        await context.Roles.AddAsync(role);
        await context.UserRoles.AddAsync(new IdentityUserRole<string>
        {
            UserId = user.Id,
            RoleId = role.Id,
        });
    }
}
