export interface PostSummary {
  id: number;
  title: string;
  contentPreview: string;
  authorId: string;
  authorName: string;
  category: string;
  isPinned: boolean;
  isLocked: boolean;
  viewCount: number;
  upvoteCount: number;
  commentCount: number;
  userVote: number;
  learningPathTitle?: string;
  createdAt: string;
}

export interface PostDetail extends PostSummary {
  content: string;
  comments: Comment[];
}

export interface PostListResponse {
  posts: PostSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface Comment {
  id: number;
  content: string;
  authorId: string;
  authorName: string;
  postId: number;
  parentCommentId?: number;
  upvoteCount: number;
  userVote: number;
  replies: Comment[];
  createdAt: string;
  updatedAt: string;
}

export interface CreatePostRequest {
  title: string;
  content: string;
  category: string;
  learningPathId?: number;
}

export interface CreateCommentRequest {
  content: string;
  parentCommentId?: number;
}

export const COMMUNITY_CATEGORIES = [
  "All",
  "General",
  "Questions",
  "Resources",
  "Projects",
  "Announcements",
] as const;

export type CommunityCategory = typeof COMMUNITY_CATEGORIES[number];
