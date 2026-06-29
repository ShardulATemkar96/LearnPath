export interface UserAnalytics {
  totalModulesCompleted: number;
  totalPathsEnrolled: number;
  totalCertificates: number;
  activeClassrooms: number;
  currentStreak: number;
  averageCompletionRate: number;
  weeklyActivity: WeeklyActivity[];
  pathCompletions: PathCompletion[];
  moduleTypeBreakdown: ModuleTypeBreakdown[];
}

export interface WeeklyActivity {
  day: string;
  modulesCompleted: number;
}

export interface PathCompletion {
  title: string;
  completedModules: number;
  totalModules: number;
  percent: number;
}

export interface ModuleTypeBreakdown {
  contentType: string;
  count: number;
}