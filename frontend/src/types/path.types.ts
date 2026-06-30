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
  contentBody?:string;
  order: number;
  learningPathId: number;
  isCompleted: boolean;
  isUnlocked: boolean;
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
}
