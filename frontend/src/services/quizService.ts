import apiClient from "./apiClient";
import {
  QuizResponseDto, AdminQuestionDto, CreateQuizRequest, CreateQuestionRequest,
  StartAttemptResponse, SubmitAnswersRequest, AttemptResultDto, AttemptSummaryDto,
} from "../types/quiz.types";

export const quizService = {
  getQuiz: async (moduleId: number): Promise<QuizResponseDto> => {
    const { data } = await apiClient.get(`/modules/${moduleId}/quiz`);
    return data.data;
  },

  createQuiz: async (moduleId: number, payload: CreateQuizRequest): Promise<QuizResponseDto> => {
    const { data } = await apiClient.post(`/modules/${moduleId}/quiz`, payload);
    return data.data;
  },

  updateQuiz: async (moduleId: number, quizId: number, payload: CreateQuizRequest): Promise<QuizResponseDto> => {
    const { data } = await apiClient.put(`/modules/${moduleId}/quiz/${quizId}`, payload);
    return data.data;
  },

  deleteQuiz: async (moduleId: number, quizId: number): Promise<void> => {
    await apiClient.delete(`/modules/${moduleId}/quiz/${quizId}`);
  },

  addQuestion: async (moduleId: number, quizId: number, payload: CreateQuestionRequest): Promise<AdminQuestionDto> => {
    const { data } = await apiClient.post(`/modules/${moduleId}/quiz/${quizId}/questions`, payload);
    return data.data;
  },

  updateQuestion: async (moduleId: number, quizId: number, questionId: number, payload: CreateQuestionRequest): Promise<AdminQuestionDto> => {
    const { data } = await apiClient.put(`/modules/${moduleId}/quiz/${quizId}/questions/${questionId}`, payload);
    return data.data;
  },

  deleteQuestion: async (moduleId: number, quizId: number, questionId: number): Promise<void> => {
    await apiClient.delete(`/modules/${moduleId}/quiz/${quizId}/questions/${questionId}`);
  },

  startAttempt: async (moduleId: number): Promise<StartAttemptResponse> => {
    const { data } = await apiClient.post(`/modules/${moduleId}/quiz/start`);
    return data.data;
  },

  submitAttempt: async (moduleId: number, payload: SubmitAnswersRequest): Promise<AttemptResultDto> => {
    const { data } = await apiClient.post(`/modules/${moduleId}/quiz/submit`, payload);
    return data.data;
  },

  getAttemptResult: async (moduleId: number, attemptId: number): Promise<AttemptResultDto> => {
    const { data } = await apiClient.get(`/modules/${moduleId}/quiz/attempts/${attemptId}/result`);
    return data.data;
  },

  getAttemptHistory: async (moduleId: number): Promise<AttemptSummaryDto[]> => {
    const { data } = await apiClient.get(`/modules/${moduleId}/quiz/attempts`);
    return data.data;
  },

  checkAvailability: async (moduleId: number): Promise<{ available: boolean; reason?: string }> => {
    const { data } = await apiClient.get(`/modules/${moduleId}/quiz/check`);
    return data.data;
  },
};
