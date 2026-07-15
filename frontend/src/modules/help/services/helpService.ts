import type {
  HelpArticleDetail,
  HelpArticleSummary,
  SaveHelpArticleRequest,
} from '@modules/help/types/help';
import { authorizedFetch } from '@platform/api/authService';

export async function getArticles(
  search?: string,
  category?: string,
): Promise<HelpArticleSummary[]> {
  const params = new URLSearchParams();
  if (search?.trim()) {
    params.set('search', search.trim());
  }
  if (category?.trim()) {
    params.set('category', category.trim());
  }

  const query = params.toString();
  return authorizedFetch<HelpArticleSummary[]>(`/api/help/articles${query ? `?${query}` : ''}`);
}

export async function getArticle(id: string): Promise<HelpArticleDetail> {
  return authorizedFetch<HelpArticleDetail>(`/api/help/articles/${encodeURIComponent(id)}`);
}

export async function getCategories(): Promise<string[]> {
  return authorizedFetch<string[]>('/api/help/categories');
}

export async function getAdminArticles(): Promise<HelpArticleSummary[]> {
  return authorizedFetch<HelpArticleSummary[]>('/api/help/admin/articles');
}

export async function getAdminArticle(id: string): Promise<HelpArticleDetail> {
  return authorizedFetch<HelpArticleDetail>(`/api/help/admin/articles/${encodeURIComponent(id)}`);
}

export async function createArticle(data: SaveHelpArticleRequest): Promise<HelpArticleDetail> {
  return authorizedFetch<HelpArticleDetail>('/api/help/admin/articles', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateArticle(
  id: string,
  data: SaveHelpArticleRequest,
): Promise<HelpArticleDetail> {
  return authorizedFetch<HelpArticleDetail>(`/api/help/admin/articles/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function togglePublish(id: string): Promise<HelpArticleDetail> {
  return authorizedFetch<HelpArticleDetail>(
    `/api/help/admin/articles/${encodeURIComponent(id)}/publish`,
    { method: 'PATCH' },
  );
}

export async function deleteArticle(id: string): Promise<void> {
  await authorizedFetch<void>(`/api/help/admin/articles/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}
