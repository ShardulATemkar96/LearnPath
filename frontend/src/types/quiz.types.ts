export enum Difficulty {
  Easy = 0,
  Medium = 1,
  Hard = 2,
}

export enum SelectionMode {
  Random = 0,
  Sequential = 1,
}

export enum QuizStatus {
  Draft = "Draft",
  Published = "Published",
  Archived = "Archived",
}

export enum AttemptStatus {
  Created = 0,
  InProgress = 1,
  Submitted = 2,
  Evaluated = 3,
  Expired = 4,
}

export interface CreateQuizDto {
  title: string;
  questionBankId: number;
  questionCount: number;
  difficultyFilter?: Difficulty;
  selectionMode: SelectionMode;
  timeLimitMinutes?: number;
  passingPercentage: number;
  maximumAttempts: number;
}

export interface UpdateQuizDto {
  title: string;
  questionBankId: number;
  questionCount: number;
  difficultyFilter?: Difficulty;
  selectionMode: SelectionMode;
  timeLimitMinutes?: number;
  passingPercentage: number;
  maximumAttempts: number;
}

export interface QuizResponseDto {
  id: number;
  title: string;
  questionBankId: number;
  questionBankTitle: string;
  questionBankVersion: number;
  questionCount: number;
  difficultyFilter: Difficulty | null;
  selectionMode: SelectionMode;
  timeLimitMinutes: number | null;
  passingPercentage: number;
  maximumAttempts: number;
  status: QuizStatus;
  createdAt: string;
  publishedAt: string | null;
  archivedAt: string | null;
}

export interface ModuleQuizResponseDto {
  id: number;
  moduleId: number;
  quizId: number;
  quizTitle: string;
  assignedBy: string;
  assignedAt: string;
  active: boolean;
}

export interface LinkQuizDto {
  quizId: number;
}

// ── Attempt DTOs ────────────────────────────────────────

export interface AttemptStartResponseDto {
  attemptId: number;
  quizId: number;
  quizTitle: string;
  moduleId: number;
  attemptNumber: number;
  status: AttemptStatus;
  startedAt: string;
  timeLimitMinutes: number | null;
  timeSpentSeconds: number | null;
  questions: AttemptQuestionDto[];
}

export interface AttemptQuestionDto {
  questionId: number;
  questionText: string;
  displayOrder: number;
  selectedOptionId: number | null;
  options: AttemptOptionDto[];
}

export interface AttemptOptionDto {
  optionId: number;
  optionText: string;
  displayOrder: number;
}

export interface SaveAnswerRequestDto {
  questionId: number;
  optionId: number;
}

export interface SubmitResponseDto {
  attemptId: number;
  score: number;
  totalQuestions: number;
  percentage: number;
  passed: boolean;
  timeSpentSeconds: number;
  attemptNumber: number;
  passingPercentage: number;
}

export interface ReviewResponseDto {
  attemptId: number;
  score: number;
  totalQuestions: number;
  percentage: number;
  passed: boolean;
  timeSpentSeconds: number;
  attemptNumber: number;
  passingPercentage: number;
  questions: ReviewQuestionDto[];
}

export interface ReviewQuestionDto {
  questionId: number;
  questionText: string;
  explanation: string | null;
  selectedOptionId: number | null;
  correctOptionId: number;
  isCorrect: boolean;
  options: ReviewOptionDto[];
}

export interface ReviewOptionDto {
  optionId: number;
  optionText: string;
  isCorrect: boolean;
  isSelected: boolean;
  displayOrder: number;
}

// ── Analytics DTOs ──────────────────────────────────────

export interface QuizAnalyticsResponseDto {
  quizId: number;
  quizTitle: string;
  totalAttempts: number;
  uniqueStudents: number;
  averageScore: number;
  passPercentage: number;
  totalPassed: number;
  totalFailed: number;
  scoreDistribution: ScoreDistributionDto[];
  questionAnalytics: QuestionAnalyticsDto[];
  mostIncorrectQuestion: QuestionAnalyticsDto | null;
  hardestQuestion: QuestionAnalyticsDto | null;
}

export interface ScoreDistributionDto {
  range: string;
  count: number;
}

export interface QuestionAnalyticsDto {
  questionId: number;
  questionText: string;
  timesAnswered: number;
  timesCorrect: number;
  successRate: number;
  difficulty: string;
}
