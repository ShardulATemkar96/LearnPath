import apiClient from "./apiClient";
import {
  CreateQuizDto, UpdateQuizDto, QuizResponseDto,
  ModuleQuizResponseDto, LinkQuizDto,
  AttemptStartResponseDto, SaveAnswerRequestDto,
  SubmitResponseDto, ReviewResponseDto,
  QuizAnalyticsResponseDto,
} from "../types/quiz.types";

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  timestamp: string;
}

export const quizService = {
  // ── Quiz CRUD ────────────────────────────────────────────
  getAll: async (): Promise<QuizResponseDto[]> => {
    const { data } = await apiClient.get<ApiResponse<QuizResponseDto[]>>("/quizzes");
    return data.data;
  },

  getById: async (id: number): Promise<QuizResponseDto> => {
    const { data } = await apiClient.get<ApiResponse<QuizResponseDto>>(`/quizzes/${id}`);
    return data.data;
  },

  create: async (payload: CreateQuizDto): Promise<QuizResponseDto> => {
    const { data } = await apiClient.post<ApiResponse<QuizResponseDto>>("/quizzes", payload);
    return data.data;
  },

  update: async (id: number, payload: UpdateQuizDto): Promise<QuizResponseDto> => {
    const { data } = await apiClient.put<ApiResponse<QuizResponseDto>>(`/quizzes/${id}`, payload);
    return data.data;
  },

  archive: async (id: number): Promise<QuizResponseDto> => {
    const { data } = await apiClient.patch<ApiResponse<QuizResponseDto>>(`/quizzes/${id}/archive`);
    return data.data;
  },

  publish: async (id: number): Promise<QuizResponseDto> => {
    const { data } = await apiClient.patch<ApiResponse<QuizResponseDto>>(`/quizzes/${id}/publish`);
    return data.data;
  },

  unpublish: async (id: number): Promise<QuizResponseDto> => {
    const { data } = await apiClient.patch<ApiResponse<QuizResponseDto>>(`/quizzes/${id}/unpublish`);
    return data.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/quizzes/${id}`);
  },

  // ── Module-Quiz Linking ──────────────────────────────────
  getModuleQuiz: async (moduleId: number): Promise<ModuleQuizResponseDto | null> => {
    const { data } = await apiClient.get<ApiResponse<ModuleQuizResponseDto | null>>(`/modules/${moduleId}/quiz`);
    return data.data;
  },

  linkQuiz: async (moduleId: number, quizId: number): Promise<ModuleQuizResponseDto> => {
    const { data } = await apiClient.post<ApiResponse<ModuleQuizResponseDto>>(`/modules/${moduleId}/quiz`, { quizId } as LinkQuizDto);
    return data.data;
  },

  unlinkQuiz: async (moduleId: number): Promise<void> => {
    await apiClient.delete(`/modules/${moduleId}/quiz`);
  },

  // ── Attempt Flow ─────────────────────────────────────────
  startAttempt: async (quizId: number, moduleId: number): Promise<AttemptStartResponseDto> => {
    const { data } = await apiClient.post<ApiResponse<AttemptStartResponseDto>>(`/quizzes/${quizId}/attempt`, null, {
      params: { moduleId },
    });
    return data.data;
  },

  getAttempt: async (attemptId: number): Promise<AttemptStartResponseDto> => {
    const { data } = await apiClient.get<ApiResponse<AttemptStartResponseDto>>(`/attempts/${attemptId}`);
    return data.data;
  },

  saveAnswer: async (attemptId: number, payload: SaveAnswerRequestDto): Promise<void> => {
    await apiClient.put(`/attempts/${attemptId}/answer`, payload);
  },

  submitAttempt: async (attemptId: number): Promise<SubmitResponseDto> => {
    const { data } = await apiClient.post<ApiResponse<SubmitResponseDto>>(`/attempts/${attemptId}/submit`);
    return data.data;
  },

  getReview: async (attemptId: number): Promise<ReviewResponseDto> => {
    const { data } = await apiClient.get<ApiResponse<ReviewResponseDto>>(`/attempts/${attemptId}/review`);
    return data.data;
  },

  // ── Analytics ────────────────────────────────────────────
  getAnalytics: async (quizId: number): Promise<QuizAnalyticsResponseDto> => {
    const { data } = await apiClient.get<ApiResponse<QuizAnalyticsResponseDto>>(`/quizzes/${quizId}/analytics`);
    return data.data;
  },
};
