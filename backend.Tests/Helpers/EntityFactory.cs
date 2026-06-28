using LearnPath.API.Entities;

namespace LearnPath.Tests.Helpers;

public static class EntityFactory
{
    public static User CreateUser(
        string? id        = null,
        string? email     = null,
        string? firstName = null,
        string? lastName  = null,
        bool    isActive  = true)
    {
        return new User
        {
            Id            = id ?? Guid.NewGuid().ToString(),
            Email         = email ?? "test@learnpath.dev",
            UserName      = email ?? "test@learnpath.dev",
            FirstName     = firstName ?? "Test",
            LastName      = lastName  ?? "User",
            IsActive      = isActive,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow,
        };
    }

    public static LearningPath CreateLearningPath(
        int?    id            = null,
        string? createdById   = null,
        string? title         = null,
        bool    isPublished   = true,
        bool    isPublic      = true)
    {
        return new LearningPath
        {
            Id          = id ?? 1,
            Title       = title ?? "Test Path",
            Description = "A test learning path",
            IsPublished = isPublished,
            IsPublic    = isPublic,
            CreatedById = createdById ?? Guid.NewGuid().ToString(),
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
        };
    }

    public static Module CreateModule(
        int     id           = 1,
        int     learningPathId = 1,
        string? title        = null,
        int     order        = 1)
    {
        return new Module
        {
            Id             = id,
            Title          = title ?? $"Module {id}",
            Description    = "Test module",
            ContentType    = "video",
            Order          = order,
            LearningPathId = learningPathId,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
        };
    }

    public static Classroom CreateClassroom(
        int     id             = 1,
        string? createdById    = null,
        int     learningPathId = 1)
    {
        return new Classroom
        {
            Id             = id,
            Title          = "Test Classroom",
            Description    = "A test classroom",
            InviteCode     = "TESTCODE",
            LearningPathId = learningPathId,
            CreatedById    = createdById ?? Guid.NewGuid().ToString(),
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
        };
    }

    public static Post CreatePost(
        int     id         = 1,
        string? authorId   = null,
        string? title      = null,
        string  category   = "General")
    {
        return new Post
        {
            Id        = id,
            Title     = title ?? "Test Post",
            Content   = "Test post content that is long enough to be meaningful.",
            AuthorId  = authorId ?? Guid.NewGuid().ToString(),
            Category  = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
