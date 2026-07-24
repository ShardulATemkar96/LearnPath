namespace LearnPath.API.Entities;

public enum ModuleStatus
{
    NotStarted = 0,
    InProgress = 1,
    PendingQuiz = 2,
    QuizAssigned = 3,
    QuizAttempted = 4,
    Passed = 5,
    AwaitingApproval = 6,
    Completed = 7,
    Archived = 8
}
