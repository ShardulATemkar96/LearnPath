export interface PostSummary {
  id: number;
  title: string;
  contentPreview: string;
  codeSnippet?: string | null;
  programmingLanguage?: string | null;
  tags?: string | null;
  authorId: string;
  authorName: string;
  authorIsDeleted: boolean;
  category: string;
  isPinned: boolean;
  isLocked: boolean;
  viewCount: number;
  upvoteCount: number;
  commentCount: number;
  userVote: number;
  learningPathTitle?: string;
  groupId?: number | null;
  groupName?: string | null;
  createdAt: string;
  editedAt?: string;
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
  authorIsDeleted: boolean;
  postId: number;
  parentCommentId?: number;
  upvoteCount: number;
  userVote: number;
  replies: Comment[];
  createdAt: string;
  updatedAt: string;
  editedAt?: string;
}

export interface CreatePostRequest {
  title: string;
  content: string;
  category: string;
  codeSnippet?: string | null;
  programmingLanguage?: string | null;
  tags?: string | null;
  learningPathId?: number;
}

export interface CreateCommentRequest {
  content: string;
  parentCommentId?: number;
}

export interface Group {
  id: number;
  name: string;
  description: string;
  ownerId: string;
  ownerName: string;
  isPublic: boolean;
  memberCount: number;
  postCount: number;
  createdAt: string;
}

export interface GroupMember {
  userId: string;
  userName: string;
  role: string;
  joinedAt: string;
}

export interface GroupDetail extends Group {
  members: GroupMember[];
  currentUserRole?: string | null;
}

export interface GroupListResponse {
  groups: Group[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateGroupRequest {
  name: string;
  description: string;
  isPublic?: boolean;
}

export interface UpdateGroupRequest {
  name: string;
  description: string;
  isPublic?: boolean;
}

export interface VoteRequest {
  isUpvote: boolean;
}

export interface CreateReportRequest {
  reason: string;
}

export interface Report {
  id: number;
  targetType: string;
  targetId: number;
  reason: string;
  status: string;
  reportedByUserId: string;
  reportedByName: string;
  createdAt: string;
}

export interface ReportListResponse {
  reports: Report[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const POST_SORT_ORDERS = ["Newest", "Score", "Trending"] as const;
export type PostSortOrder = typeof POST_SORT_ORDERS[number];

export const POST_FILTERS = [
  "All Posts",
  "My Posts",
  "Pinned",
  "With Code",
  "Without Code",
] as const;

export type PostFilter = typeof POST_FILTERS[number];

export const COMMUNITY_CATEGORIES = [
  "All",
  "General",
  "Questions",
  "Resources",
  "Projects",
  "Announcements",
] as const;

export type CommunityCategory = typeof COMMUNITY_CATEGORIES[number];

export const PROGRAMMING_LANGUAGES = [
  "typescript",
  "javascript",
  "jsx",
  "tsx",
  "python",
  "c",
  "cpp",
  "go",
  "rust",
  "swift",
  "kotlin",
  "objectivec",
  "sql",
  "json",
  "css",
  "html",
  "markdown",
  "yaml",
  "graphql",
] as const;

export type ProgrammingLanguage = typeof PROGRAMMING_LANGUAGES[number];
