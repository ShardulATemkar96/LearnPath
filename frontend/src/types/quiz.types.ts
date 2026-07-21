export enum QuizQuestionType {
  MultipleChoice = 0,
  TrueFalse = 1,
  ShortAnswer = 2,
}

export interface QuizResponseDto {
  id: number;
  moduleId: number;
  title: string;
  description?: string;
  passingScore?: number;
  timeLimitMinutes?: number;
  maxAttempts: number;
  shuffleQuestions: boolean;
  showResults: boolean;
  isMandatory: boolean;
  isPublished: boolean;
  questionCount: number;
  questions: AdminQuestionDto[];
}

export interface AdminQuestionDto {
  id: number;
  questionText: string;
  questionType: QuizQuestionType;
  points: number;
  orderIndex: number;
  explanation?: string;
  options: AdminOptionDto[];
}

export interface AdminOptionDto {
  id: number;
  optionText: string;
  isCorrect: boolean;
  orderIndex: number;
}

export interface CreateQuizRequest {
  title: string;
  description?: string;
  passingScore?: number;
  timeLimitMinutes?: number;
  maxAttempts: number;
  shuffleQuestions: boolean;
  showResults: boolean;
  isMandatory: boolean;
}

export interface CreateQuestionRequest {
  questionText: string;
  questionType: QuizQuestionType;
  points: number;
  orderIndex: number;
  explanation?: string;
  options: CreateOptionRequest[];
}

export interface CreateOptionRequest {
  optionText: string;
  isCorrect: boolean;
  orderIndex: number;
}

export interface StartAttemptResponse {
  attemptId: number;
  timeLimitMinutes?: number;
  questions: AttemptQuestionDto[];
}

export interface AttemptQuestionDto {
  questionId: number;
  questionText: string;
  questionType: QuizQuestionType;
  points: number;
  orderIndex: number;
  options: AttemptOptionDto[];
}

export interface AttemptOptionDto {
  id: number;
  optionText: string;
  orderIndex: number;
}

export interface SubmitAnswersRequest {
  attemptId: number;
  answers: AnswerSubmissionDto[];
}

export interface AnswerSubmissionDto {
  questionId: number;
  selectedOptionId?: number;
  textAnswer?: string;
}

export interface AttemptResultDto {
  attemptId: number;
  score: number;
  totalPoints: number;
  isPassed: boolean;
  status: string;
  attemptNumber: number;
  maxAttempts: number;
  questionResults: QuestionResultDto[];
}

export interface QuestionResultDto {
  questionId: number;
  questionText: string;
  questionType: QuizQuestionType;
  points: number;
  pointsAwarded: number;
  isCorrect: boolean;
  selectedOptionId?: number;
  correctOptionId?: number;
  textAnswer?: string;
  correctAnswerText?: string;
  explanation?: string;
}

export interface AttemptSummaryDto {
  attemptId: number;
  score: number;
  totalPoints: number;
  isPassed: boolean;
  status: string;
  startedAt: string;
  completedAt?: string;
}
