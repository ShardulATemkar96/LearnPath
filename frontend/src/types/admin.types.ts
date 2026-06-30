export interface AdminUser {
  userId: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
  totalPathsCreated: number;
  totalModulesCompleted: number;
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
