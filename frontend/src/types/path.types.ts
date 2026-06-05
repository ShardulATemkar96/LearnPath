export interface LearningPath {
  id: number;
  title: string;
  description: string;
  thumbnailUrl?: string;
  isPublished: boolean;
  isPublic: boolean;
  createdById: string;
  createdAt: string;
}

export interface Module {
  id: number;
  title: string;
  description: string;
  contentUrl?: string;
  contentType: string;
  order: number;
  learningPathId: number;
}