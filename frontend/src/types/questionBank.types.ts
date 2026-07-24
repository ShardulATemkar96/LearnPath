export enum QuestionBankStatus {
  Draft = "Draft",
  Active = "Active",
  Archived = "Archived",
}

export interface QuestionBankSummaryDto {
  id: number;
  title: string;
  subject: string;
  tags: string | null;
  version: number;
  originalFileName: string;
  questionCount: number;
  status: QuestionBankStatus;
  createdBy: string;
  createdAt: string;
  archivedAt: string | null;
}

export interface QuestionBankSearchResult {
  success: boolean;
  message: string;
  data: {
    items: QuestionBankSummaryDto[];
    totalCount: number;
  };
  errors: string[];
}
