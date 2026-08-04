import apiClient from "./apiClient";
import {
  AdminUser, AdminStats, AdminPath,
  AdminCertificate, AdminCertificateList, AdminCertificateQuery,
  AdminClassroom, AdminClassroomList, AdminClassroomQuery, AdminClassroomDetail,
} from "../types/admin.types";

export const adminService = {
  getStats: async (): Promise<AdminStats> => {
    const { data } = await apiClient.get("/admin/stats");
    return data.data;
  },

  getUsers: async (search?: string): Promise<AdminUser[]> => {
    const { data } = await apiClient.get("/admin/users", {
      params: search ? { search } : undefined,
    });
    return data.data;
  },

  getUser: async (userId: string): Promise<AdminUser> => {
    const { data } = await apiClient.get(`/admin/users/${userId}`);
    return data.data;
  },

  updateRole: async (userId: string, role: string): Promise<AdminUser> => {
    const { data } = await apiClient.put(`/admin/users/${userId}/role`, { role });
    return data.data;
  },

  activateUser: async (userId: string): Promise<AdminUser> => {
    const { data } = await apiClient.put(`/admin/users/${userId}/activate`);
    return data.data;
  },

  deactivateUser: async (userId: string): Promise<AdminUser> => {
    const { data } = await apiClient.put(`/admin/users/${userId}/deactivate`);
    return data.data;
  },

  markInvalid: async (userId: string, reason: string): Promise<AdminUser> => {
    const { data } = await apiClient.put(`/admin/users/${userId}/invalid`, { reason });
    return data.data;
  },

  deleteUser: async (userId: string): Promise<void> => {
    await apiClient.delete(`/admin/users/${userId}`);
  },

  getAllPaths: async (): Promise<AdminPath[]> => {
    const { data } = await apiClient.get("/admin/paths");
    return data.data;
  },

  getCertificates: async (query: AdminCertificateQuery = {}): Promise<AdminCertificateList> => {
    const { data } = await apiClient.get("/admin/certificates", { params: query });
    return data.data;
  },

  getCertificate: async (id: number): Promise<AdminCertificate> => {
    const { data } = await apiClient.get(`/admin/certificates/${id}`);
    return data.data;
  },

  deleteCertificate: async (id: number): Promise<void> => {
    await apiClient.delete(`/admin/certificates/${id}`);
  },

  getClassrooms: async (query: AdminClassroomQuery = {}): Promise<AdminClassroomList> => {
    const { data } = await apiClient.get("/admin/classrooms", { params: query });
    return data.data;
  },

  getClassroom: async (id: number): Promise<AdminClassroomDetail> => {
    const { data } = await apiClient.get(`/admin/classrooms/${id}`);
    return data.data;
  },

  updateClassroom: async (
    id: number,
    payload: { title: string; description: string; learningPathId?: number; trainerId?: string },
  ): Promise<AdminClassroom> => {
    const { data } = await apiClient.put(`/admin/classrooms/${id}`, payload);
    return data.data;
  },

  reassignClassroomLearningPath: async (id: number, learningPathId: number): Promise<AdminClassroom> => {
    const { data } = await apiClient.put(`/admin/classrooms/${id}/learning-path`, { learningPathId });
    return data.data;
  },

  deleteClassroom: async (id: number): Promise<void> => {
    await apiClient.delete(`/admin/classrooms/${id}`);
  },
};
