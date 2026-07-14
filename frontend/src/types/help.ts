export interface HelpArticleSummary {
  id: string;
  title: string;
  slug: string;
  categoryName: string | null;
  tags: string[];
  isPublished: boolean;
  updatedAt: string;
}

export interface HelpArticleDetail extends HelpArticleSummary {
  body: string;
  createdAt: string;
}

export interface SaveHelpArticleRequest {
  title: string;
  body: string;
  categoryName: string | null;
  tags: string[];
  isPublished: boolean;
}
