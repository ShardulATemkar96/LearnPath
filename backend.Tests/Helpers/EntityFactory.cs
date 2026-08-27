using LearnPath.API.Entities;

namespace LearnPath.Tests.Helpers;

public static class EntityFactory
{
    /* ── Quiz Engine Entities ────────────────────────────── */

    public static QuestionBank CreateQuestionBank(
        int?    id            = null,
        string? title         = null,
        string? subject       = null,
        int     version       = 1,
        int     questionCount = 5,
        string? createdBy     = null,
        QuestionBankStatus status = QuestionBankStatus.Draft)
    {
        var bank = new QuestionBank
        {
            Id             = id ?? 1,
            Title          = title ?? "Test Question Bank",
            Subject        = subject ?? "Java",
            Tags           = "java,oop",
            Version        = version,
            OriginalFileName = "test.json",
            StoredJson     = "{\"title\":\"Test\"}",
            QuestionCount  = questionCount,
            Status         = status,
            CreatedBy      = createdBy ?? Guid.NewGuid().ToString(),
            CreatedAt      = DateTime.UtcNow,
        };
        for (int i = 1; i <= questionCount; i++)
            bank.Questions.Add(CreateQuestion(id: i, questionBankId: bank.Id, difficulty: (Difficulty)(i % 3)));
        return bank;
    }

    public static Question CreateQuestion(
        int    id              = 1,
        int    questionBankId  = 1,
        string? questionText   = null,
        Difficulty difficulty  = Difficulty.Easy)
    {
        var q = new Question
        {
            Id              = id,
            QuestionBankId  = questionBankId,
            QuestionText    = questionText ?? $"Test Question {id}",
            Difficulty      = difficulty,
            Explanation     = $"Explanation for question {id}",
            CreatedAt       = DateTime.UtcNow,
        };
        q.Options.Add(CreateOption(id: id * 10 + 1, questionId: id, optionText: "Option A", isCorrect: true, displayOrder: 0));
        q.Options.Add(CreateOption(id: id * 10 + 2, questionId: id, optionText: "Option B", isCorrect: false, displayOrder: 1));
        q.Options.Add(CreateOption(id: id * 10 + 3, questionId: id, optionText: "Option C", isCorrect: false, displayOrder: 2));
        return q;
    }

    public static Option CreateOption(
        int     id           = 1,
        int     questionId   = 1,
        string? optionText   = null,
        bool    isCorrect    = false,
        int     displayOrder = 0)
    {
        return new Option
        {
            Id           = id,
            QuestionId   = questionId,
            OptionText   = optionText ?? $"Option {id}",
            IsCorrect    = isCorrect,
            DisplayOrder = displayOrder,
        };
    }

    public static Quiz CreateQuiz(
        int?   id              = null,
        string? title          = null,
        int    questionBankId  = 1,
        int    questionCount   = 5,
        QuizStatus status      = QuizStatus.Published,
        string? createdById    = null)
    {
        return new Quiz
        {
            Id               = id ?? 1,
            Title            = title ?? "Test Quiz",
            QuestionBankId   = questionBankId,
            QuestionCount    = questionCount,
            DifficultyFilter = null,
            SelectionMode    = SelectionMode.Random,
            TimeLimitMinutes = 30,
            PassingPercentage = 40,
            MaximumAttempts  = 3,
            Status           = status,
            CreatedById      = createdById,
            CreatedAt        = DateTime.UtcNow,
        };
    }

    public static ModuleQuiz CreateModuleQuiz(
        int    id          = 1,
        int    moduleId    = 1,
        int    quizId      = 1,
        string? assignedBy = null)
    {
        return new ModuleQuiz
        {
            Id         = id,
            ModuleId   = moduleId,
            QuizId     = quizId,
            AssignedBy = assignedBy ?? Guid.NewGuid().ToString(),
            AssignedAt = DateTime.UtcNow,
            Active     = true,
        };
    }

    public static QuizAttempt CreateQuizAttempt(
        int    id            = 1,
        string? userId       = null,
        int    quizId        = 1,
        int    moduleId      = 1,
        int    attemptNumber = 1,
        AttemptStatus status = AttemptStatus.InProgress,
        int    randomSeed    = 42,
        int?   score         = null,
        decimal? percentage  = null,
        bool?  passed        = null)
    {
        return new QuizAttempt
        {
            Id            = id,
            UserId        = userId ?? Guid.NewGuid().ToString(),
            QuizId        = quizId,
            ModuleId      = moduleId,
            AttemptNumber = attemptNumber,
            StartedAt     = DateTime.UtcNow,
            Status        = status,
            RandomSeed    = randomSeed,
            Score         = score,
            Percentage    = percentage,
            Passed        = passed,
        };
    }

    public static StudentAnswer CreateStudentAnswer(
        int    id          = 1,
        int    attemptId   = 1,
        int    questionId  = 1,
        int    optionId    = 11)
    {
        return new StudentAnswer
        {
            Id            = id,
            QuizAttemptId = attemptId,
            QuestionId    = questionId,
            OptionId      = optionId,
            AnsweredAt    = DateTime.UtcNow,
        };
    }

    /* ── Original Entity Factories ── */
    public static User CreateUser(
        string?    id        = null,
        string?    email     = null,
        string?    firstName = null,
        string?    lastName  = null,
        UserStatus status    = UserStatus.Active)
    {
        return new User
        {
            Id            = id ?? Guid.NewGuid().ToString(),
            Email         = email ?? "test@learnpath.dev",
            UserName      = email ?? "test@learnpath.dev",
            FirstName     = firstName ?? "Test",
            LastName      = lastName  ?? "User",
            Status        = status,
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
