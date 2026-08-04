export interface HistoryEntry {
  id: number;
  timestamp: string;
  actionType: number;
  actionName: string;
  category: string;
  entityType: string;
  entityId?: string | null;
  description: string;
  additionalData?: string | null;
}

export interface HistoryListResponse {
  entries: HistoryEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface HistoryActionOption {
  actionType: number;
  actionName: string;
  category: string;
}

export interface AdminHistoryEntry extends HistoryEntry {
  userId?: string | null;
  username?: string | null;
  role?: string | null;
  /** null when the entity type has no destination to navigate to. */
  entityExists?: boolean | null;
}

export interface AdminHistoryListResponse {
  entries: AdminHistoryEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AdminHistoryQuery {
  userId?: string;
  role?: string;
  actionType?: number;
  entityType?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
  sort?: "newest" | "oldest";
  page?: number;
  pageSize?: number;
}

export interface HistoryQuery {
  actionType?: number;
  fromDate?: string;
  toDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
