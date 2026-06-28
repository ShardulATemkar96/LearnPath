using FluentAssertions;
using LearnPath.API.DTOs.Community;
using LearnPath.API.Services.Community;
using LearnPath.Tests.Helpers;
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
        var service = new CommunityService(context);

        // Act
        var result = await service.GetPostsAsync(
            null, null, 1, 10, "any-user");

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

        var service = new CommunityService(context);

        // Act
        var result = await service.GetPostsAsync(
            null, null, page: 1, pageSize: 10, userId: authorId);

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

        var service = new CommunityService(context);

        // Act
        var result = await service.GetPostsAsync(
            "Questions", null, 1, 10, authorId);

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

        var service = new CommunityService(context);
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

        var service = new CommunityService(context);

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

        var service = new CommunityService(context);

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

        var service = new CommunityService(context);

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

        var service = new CommunityService(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddCommentAsync(
                1, new CreateCommentDto { Content = "Hello" }, authorId));
        ex.Message.Should().Contain("locked");
    }
}
