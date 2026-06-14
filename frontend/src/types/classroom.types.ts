export interface Classroom {
  id: number;
  title: string;
  description: string;
  inviteCode: string;
  learningPathId: number;
  learningPathTitle: string;
  createdById: string;
  createdByName: string;
  memberCount: number;
  userRole: string;
  createdAt: string;
}

export interface ClassroomDetail extends Classroom {
  members: ClassroomMember[];
  assignments: Assignment[];
}

export interface ClassroomMember {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  joinedAt: string;
}

export interface Assignment {
  id: number;
  title: string;
  description: string;
  dueDate: string;
  classroomId: number;
  submissionCount: number;
  hasSubmitted: boolean;
  createdAt: string;
}

export interface Submission {
  id: number;
  assignmentId: number;
  userId: string;
  userFullName: string;
  contentUrl: string;
  feedback?: string;
  grade?: number;
  submittedAt: string;
}

export interface CreateClassroomRequest {
  title: string;
  description: string;
  learningPathId: number;
}

export interface CreateAssignmentRequest {
  title: string;
  description: string;
  dueDate: string;
}