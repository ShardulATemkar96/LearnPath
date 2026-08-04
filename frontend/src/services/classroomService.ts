import apiClient from "./apiClient";
import {
  Classroom, ClassroomDetail, Assignment,
  Submission, AiFeedbackResponse, CreateClassroomRequest, CreateAssignmentRequest,
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

  removeMember: async (classroomId: number, memberUserId: string): Promise<void> => {
    await apiClient.delete(`/classrooms/${classroomId}/members/${memberUserId}`);
  },

  markInvalid: async (classroomId: number, memberUserId: string, reason: string): Promise<void> => {
    await apiClient.post(`/classrooms/${classroomId}/members/${memberUserId}/invalid`, { reason });
  },

  createAssignment: async (
    classroomId: number, payload: CreateAssignmentRequest
  ): Promise<Assignment> => {
    const { data } = await apiClient.post(
      `/classrooms/${classroomId}/assignments`, payload
    );
    return data.data;
  },

  updateAssignment: async (
    classroomId: number, assignmentId: number, payload: CreateAssignmentRequest
  ): Promise<Assignment> => {
    const { data } = await apiClient.put(
      `/classrooms/${classroomId}/assignments/${assignmentId}`, payload
    );
    return data.data;
  },

  deleteAssignment: async (classroomId: number, assignmentId: number): Promise<void> => {
    await apiClient.delete(`/classrooms/${classroomId}/assignments/${assignmentId}`);
  },

  uploadSubmission: async (
    classroomId: number, assignmentId: number, file: File
  ): Promise<Submission> => {
    const formData = new FormData();
    formData.append("file", file);
    const { data } = await apiClient.post(
      `/classrooms/${classroomId}/assignments/${assignmentId}/submit`,
      formData,
      { headers: { "Content-Type": undefined } }
    );
    return data.data;
  },

  getMySubmission: async (
    classroomId: number, assignmentId: number
  ): Promise<Submission> => {
    const { data } = await apiClient.get(
      `/classrooms/${classroomId}/assignments/${assignmentId}/submission`
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

  deleteSubmission: async (classroomId: number, assignmentId: number): Promise<void> => {
    await apiClient.delete(`/classrooms/${classroomId}/assignments/${assignmentId}/submissions/mine`);
  },

  verifySubmission: async (classroomId: number, assignmentId: number, submissionId: number): Promise<Submission> => {
    const { data } = await apiClient.put(`/classrooms/${classroomId}/assignments/${assignmentId}/submissions/${submissionId}/verify`);
    return data.data;
  },

  completeSubmission: async (classroomId: number, assignmentId: number, submissionId: number): Promise<Submission> => {
    const { data } = await apiClient.put(`/classrooms/${classroomId}/assignments/${assignmentId}/submissions/${submissionId}/complete`);
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

  getSubmissionById: async (submissionId: number): Promise<Submission> => {
    const { data } = await apiClient.get(`/submissions/${submissionId}`);
    return data.data;
  },

  previewSubmissionFile: async (submissionId: number): Promise<Blob> => {
    const { data } = await apiClient.get(`/submissions/${submissionId}/file`, {
      responseType: "blob",
    });
    return data;
  },

  downloadSubmissionFile: async (submissionId: number): Promise<{
    blob: Blob; fileName: string; mimeType: string;
  }> => {
    const response = await apiClient.get(
      `/submissions/${submissionId}/file/download`,
      { responseType: "blob" }
    );
    const disposition = response.headers["content-disposition"] ?? "";
    const match = disposition.match(/filename="?(.+?)"?$/);
    const fileName = match?.[1] ?? "download";
    return {
      blob: response.data,
      fileName,
      mimeType: response.headers["content-type"] ?? "application/octet-stream",
    };
  },

  transitionStatus: async (
    submissionId: number, status: string
  ): Promise<Submission> => {
    const { data } = await apiClient.patch(
      `/submissions/${submissionId}/status`, { status }
    );
    return data.data;
  },

  returnForResubmission: async (
    submissionId: number, feedback?: string
  ): Promise<Submission> => {
    const { data } = await apiClient.post(
      `/submissions/${submissionId}/return`, { feedback }
    );
    return data.data;
  },

  getAiFeedback: async (submissionId: number): Promise<AiFeedbackResponse> => {
    const { data } = await apiClient.get(`/submissions/${submissionId}/ai-feedback`);
    return data.data;
  },

  publishEvaluation: async (classroomId: number, assignmentId: number, submissionId: number): Promise<Submission> => {
    const { data } = await apiClient.post(
      `/classrooms/${classroomId}/assignments/${assignmentId}/submissions/${submissionId}/publish`
    );
    return data.data;
  },

  generateAiFeedback: async (submissionId: number): Promise<AiFeedbackResponse> => {
    const { data } = await apiClient.post(`/submissions/${submissionId}/ai-feedback`);
    return data.data;
  },
};
