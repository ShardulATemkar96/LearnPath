tsexport interface AdminUser {
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
