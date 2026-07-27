import apiClient from "./apiClient";
import { QuestionBankSummaryDto, QuestionBankStatus } from "../types/questionBank.types";

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  timestamp: string;
}

interface SearchResult {
  items: QuestionBankSummaryDto[];
  totalCount: number;
}

export const questionBankService = {
  upload: async (file: File): Promise<QuestionBankSummaryDto> => {
    const formData = new FormData();
    formData.append("file", file);
    const { data } = await apiClient.post<ApiResponse<{ success: boolean; questionBank: QuestionBankSummaryDto; message: string }>>(
      "/questionbanks/upload", formData, { headers: { "Content-Type": undefined } }
    );
    return data.data.questionBank;
  },

  search: async (params?: {
    search?: string;
    subject?: string;
    status?: QuestionBankStatus;
    page?: number;
    pageSize?: number;
  }): Promise<SearchResult> => {
    const { data } = await apiClient.get<ApiResponse<QuestionBankSummaryDto[]>>("/questionbanks", {
      params: params?.search ? { title: params.search } : undefined,
    });
    const items = data.data ?? [];
    return { items, totalCount: items.length };
  },

  getById: async (id: number): Promise<QuestionBankSummaryDto> => {
    const { data } = await apiClient.get<ApiResponse<QuestionBankSummaryDto>>(`/questionbanks/${id}`);
    return data.data;
  },

  download: async (id: number): Promise<Blob> => {
    const response = await apiClient.get(`/questionbanks/${id}/download`, {
      responseType: "blob",
    });
    return response.data;
  },

  uploadVersion: async (id: number, file: File): Promise<QuestionBankSummaryDto> => {
    const formData = new FormData();
    formData.append("file", file);
    const { data } = await apiClient.post<ApiResponse<{ success: boolean; questionBank: QuestionBankSummaryDto; message: string }>>(
      `/questionbanks/${id}/version`, formData, { headers: { "Content-Type": undefined } }
    );
    return data.data.questionBank;
  },

  archive: async (id: number): Promise<QuestionBankSummaryDto> => {
    const { data } = await apiClient.patch<ApiResponse<QuestionBankSummaryDto>>(`/questionbanks/${id}/archive`);
    return data.data;
  },

  restore: async (id: number): Promise<QuestionBankSummaryDto> => {
    const { data } = await apiClient.patch<ApiResponse<QuestionBankSummaryDto>>(`/questionbanks/${id}/restore`);
    return data.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/questionbanks/${id}`);
  },
};
