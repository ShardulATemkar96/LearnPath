import apiClient from "./apiClient";
import {
  LearningPath, LearningPathDetail,
  Module, CreateLearningPathRequest, CreateModuleRequest,
} from "../types/path.types";

export const pathService = {
  getPublic: async (): Promise<LearningPath[]> => {
    const { data } = await apiClient.get("/paths/public");
    return data.data;
  },

  getMyPaths: async (): Promise<LearningPath[]> => {
    const { data } = await apiClient.get("/paths/my");
    return data.data;
  },

  getById: async (id: number): Promise<LearningPathDetail> => {
    const { data } = await apiClient.get(`/paths/${id}`);
    return data.data;
  },

  create: async (payload: CreateLearningPathRequest): Promise<LearningPath> => {
    const { data } = await apiClient.post("/paths", payload);
    return data.data;
  },

  update: async (id: number, payload: Partial<CreateLearningPathRequest>): Promise<LearningPath> => {
    const { data } = await apiClient.put(`/paths/${id}`, payload);
    return data.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/paths/${id}`);
  },

  addModule: async (pathId: number, payload: CreateModuleRequest): Promise<Module> => {
    const { data } = await apiClient.post(`/paths/${pathId}/modules`, payload);
    return data.data;
  },

  deleteModule: async (pathId: number, moduleId: number): Promise<void> => {
    await apiClient.delete(`/paths/${pathId}/modules/${moduleId}`);
  },

  addDependency: async (
    pathId: number, moduleId: number, dependsOnModuleId: number
  ): Promise<void> => {
    await apiClient.post(`/paths/${pathId}/dependencies`, { moduleId, dependsOnModuleId });
  },

  removeDependency: async (
    pathId: number, moduleId: number, dependsOnModuleId: number
  ): Promise<void> => {
    await apiClient.delete(`/paths/${pathId}/dependencies/${moduleId}/${dependsOnModuleId}`);
  },
};
