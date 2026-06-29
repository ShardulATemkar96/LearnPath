import apiClient from "./apiClient";

export interface Certificate {
  id: number;
  learningPathId: number;
  learningPathTitle: string;
  certificateUrl: string;
  issuedAt: string;
}

export const certificateService = {
  getMy: async (): Promise<Certificate[]> => {
    const { data } = await apiClient.get("/certificates");
    return data.data;
  },
};
