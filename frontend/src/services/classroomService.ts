import apiClient from "./apiClient";
import {
  Classroom, ClassroomDetail, Assignment,
  Submission, CreateClassroomRequest, CreateAssignmentRequest,
} from "../types/classroom.types";

export const classroomService = {
  getMy: async (): Promise<Classroom[]> => {
    const { data } = await apiClient.get("/classrooms");
    return data.data;
  },

  getById: async (id: number): Promise<ClassroomDetail> => {
    const { data } = await apiClient.get(`/classrooms/${id}`);
    return data.data;
  },

  create: async (payload: CreateClassroomRequest): Promise<Classroom> => {
    const { data } = await apiClient.post("/classrooms", payload);
    return data.data;
  },

  update: async (id: number, payload: Partial<CreateClassroomRequest>): Promise<Classroom> => {
    const { data } = await apiClient.put(`/classrooms/${id}`, payload);
    return data.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/classrooms/${id}`);
  },

  join: async (inviteCode: string): Promise<void> => {
    await apiClient.post("/classrooms/join", { inviteCode });
  },

  leave: async (id: number): Promise<void> => {
    await apiClient.post(`/classrooms/${id}/leave`);
  },

  createAssignment: async (
    classroomId: number, payload: CreateAssignmentRequest
  ): Promise<Assignment> => {
    const { data } = await apiClient.post(
      `/classrooms/${classroomId}/assignments`, payload
    );
    return data.data;
  },

  deleteAssignment: async (classroomId: number, assignmentId: number): Promise<void> => {
    await apiClient.delete(`/classrooms/${classroomId}/assignments/${assignmentId}`);
  },

  submit: async (
    classroomId: number, assignmentId: number, contentUrl: string
  ): Promise<Submission> => {
    const { data } = await apiClient.post(
      `/classrooms/${classroomId}/assignments/${assignmentId}/submit`,
      { contentUrl }
    );
    return data.data;
  },

  getSubmissions: async (
    classroomId: number, assignmentId: number
  ): Promise<Submission[]> => {
    const { data } = await apiClient.get(
      `/classrooms/${classroomId}/assignments/${assignmentId}/submissions`
    );
    return data.data;
  },

  gradeSubmission: async (
    classroomId: number, assignmentId: number,
    submissionId: number, grade: number, feedback?: string
  ): Promise<Submission> => {
    const { data } = await apiClient.put(
      `/classrooms/${classroomId}/assignments/${assignmentId}/submissions/${submissionId}/grade`,
      { grade, feedback }
    );
    return data.data;
  },
};