export interface LearningPath {
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

export interface LearningPathDetail extends LearningPath {
  modules: Module[];
  dependencies: ModuleDependency[];
}

export interface Module {
  id: number;
  title: string;
  description: string;
  contentUrl?: string;
  contentType: string;
  contentBody?: string;
  order: number;
  learningPathId: number;
  isCompleted: boolean;
  isUnlocked: boolean;
  difficulty: number;
  estimatedDurationMinutes?: number;
  notesHtml?: string;
  pdfUrl?: string;
  thumbnailUrl?: string;
  isDraft: boolean;
  isArchived: boolean;
  archivedAt?: string;
  quizEnabled: boolean;
  quizQuestionCount: number;
  quizPassingScore: number;
  quizTimeLimitMinutes?: number;
  resources: ResourceItem[];
  objectives: ObjectiveItem[];
  tags: string[];
}

export interface ModuleDependency {
  moduleId: number;
  dependsOnModuleId: number;
}

export interface CreateLearningPathRequest {
  title: string;
  description: string;
  thumbnailUrl?: string;
  isPublic: boolean;
}

export interface CreateModuleRequest {
  title: string;
  description: string;
  contentUrl?: string;
  contentType: string;
  order: number;
  difficulty: number; // 0=Beginner, 1=Intermediate, 2=Advanced
  estimatedDurationMinutes?: number;
  notesHtml?: string;
  pdfUrl?: string;
  thumbnailUrl?: string;
  isDraft: boolean;
  quizEnabled: boolean;
  quizQuestionCount: number;
  quizPassingScore: number;
  quizTimeLimitMinutes?: number;
  resources: { type: string; title: string; url: string; orderIndex: number }[];
  objectives: { objectiveText: string; orderIndex: number }[];
  tags: string[];
}
export interface ResourceItem {
  id: number;
  type: string;
  title: string;
  url: string;
  orderIndex: number;
}

export interface ObjectiveItem {
  id: number;
  objectiveText: string;
  orderIndex: number;
}