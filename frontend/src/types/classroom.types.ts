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
  mySubmissionId?: number;
  myOriginalFileName?: string;
  mySubmissionStatus?: string;
  myGrade?: number;
  myFeedback?: string;
  createdAt: string;
}

export interface Submission {
  id: number;
  assignmentId: number;
  userId: string;
  userFullName: string;
  originalFileName?: string;
  fileExtension?: string;
  fileSize?: number;
  mimeType?: string;
  isLate?: boolean;
  status: string;
  feedback?: string;
  grade?: number;
  submittedAt: string;
  publishedAt?: string;
  aiFeedback?: AiFeedbackResponse;
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

export interface SuggestedScoreDto {
  percentage: number;
  marks: number;
}

export interface AiFeedbackResponse {
  summary: string;
  grammarFeedback: string;
  rubricCoverage: string;
  missingTopics: string;
  suggestedScore: SuggestedScoreDto;
  overallRecommendation: string;
  disclaimer: string;
  generatedAt: string;
}

export const SubmissionStatus = {
  NotSubmitted: "NOT_SUBMITTED",
  Submitted: "SUBMITTED",
  UnderReview: "UNDER_REVIEW",
  Reviewed: "REVIEWED",
  Graded: "GRADED",
  ReturnedForResubmission: "RETURNED_FOR_RESUBMISSION",
  SubmittedAgain: "SUBMITTED_AGAIN",
} as const;
