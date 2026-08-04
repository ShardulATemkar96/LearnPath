export type UserStatus = "Active" | "Inactive" | "Deleted" | "Invalid";

export interface AdminUser {
  userId: string;
  fullName: string;
  userName: string;
  email: string;
  avatarUrl?: string | null;
  phoneNumber?: string | null;
  bio?: string | null;
  status: UserStatus;
  invalidReason?: string | null;
  isSuperAdmin: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt?: string | null;
  totalPathsCreated: number;
  totalModulesCompleted: number;
  totalQuizAttempts: number;
  totalCertificates: number;
  totalClassroomsJoined: number;
}

export interface AdminStats {
  totalUsers: number;
  totalLearningPaths: number;
  totalClassrooms: number;
  totalCertificatesIssued: number;
  totalModulesCompleted: number;
  newUsersThisMonth: number;
  userGrowth: { month: string; count: number }[];
}

export interface AdminPath {
  id: number;
  title: string;
  description: string;
  thumbnailUrl?: string;
  isPublished: boolean;
  isPublic: boolean;
  createdById: string;
  createdByName: string;
  totalModules: number;
  createdAt: string;
  updatedAt: string;
}

export interface AdminCertificate {
  id: number;
  userId: string;
  userName: string;
  userEmail: string;
  learningPathId: number;
  learningPathTitle: string;
  certificateNumber: string;
  certificateUrl: string;
  issuedAt: string;
  completedAt: string;
  status: string;
}

export interface AdminCertificateList {
  entries: AdminCertificate[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminCertificateQuery {
  search?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminClassroom {
  id: number;
  title: string;
  description: string;
  inviteCode: string;
  learningPathId: number;
  learningPathTitle: string;
  createdById: string;
  createdByName: string;
  createdByEmail: string;
  memberCount: number;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface AdminClassroomList {
  entries: AdminClassroom[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminClassroomQuery {
  search?: string;
  learningPathId?: number;
  status?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminClassroomMember {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  status: string;
  invalidReason?: string | null;
  joinedAt: string;
}

export interface AdminClassroomDetail extends AdminClassroom {
  members: AdminClassroomMember[];
  assignmentCount: number;
}
