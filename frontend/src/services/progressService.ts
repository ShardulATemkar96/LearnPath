import apiClient from "./apiClient";

export interface PathProgressSummary {
  learningPathId: number;
  title: string;
  totalModules: number;
  completedModules: number;
  progressPercent: number;
  isCertificateEligible: boolean;
  modules: ModuleProgress[];
}

export interface ModuleProgress {
  moduleId: number;
  title: string;
  isCompleted: boolean;
  isUnlocked: boolean;
  completedAt?: string;
}

export const progressService = {
  markComplete: async (moduleId: number): Promise<void> => {
    await apiClient.post("/progress/complete", { moduleId });
  },

  getMyProgress: async (): Promise<PathProgressSummary[]> => {
    const { data } = await apiClient.get("/progress");
    return data.data;
  },

  getPathProgress: async (pathId: number): Promise<PathProgressSummary> => {
    const { data } = await apiClient.get(`/progress/paths/${pathId}`);
    return data.data;
  },
};
