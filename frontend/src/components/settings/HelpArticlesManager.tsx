import { useCallback, useEffect, useMemo, useState } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  Badge,
  Button,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  MessageBar,
  MessageBarBody,
  Spinner,
  Subtitle2,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { AddRegular } from '@fluentui/react-icons';
import { ApiError } from '../../services/apiClient';
import { getAdminArticle, getAdminArticles } from '../../services/helpService';
import type { HelpArticleDetail, HelpArticleSummary } from '../../types/help';
import { HelpArticleFormDrawer } from './HelpArticleFormDrawer';

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function HelpArticlesManager() {
  const [articles, setArticles] = useState<HelpArticleSummary[]>([]);
  const [selectedArticle, setSelectedArticle] = useState<HelpArticleDetail | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  function closeDrawer() {
    setIsDrawerOpen(false);
    setSelectedArticle(null);
    setIsCreating(false);
  }

  const loadArticles = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getAdminArticles();
      setArticles(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load articles.');
      setArticles([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadArticles();
  }, [loadArticles]);

  const existingCategories = useMemo(
    () => [...new Set(articles.map((article) => article.categoryName).filter(Boolean) as string[])],
    [articles],
  );

  async function openArticle(id: string) {
    setIsCreating(false);
    setError(null);

    try {
      const article = await getAdminArticle(id);
      setSelectedArticle(article);
      setIsDrawerOpen(true);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load article.');
    }
  }

  const columns: TableColumnDefinition<HelpArticleSummary>[] = useMemo(
    () => [
      createTableColumn<HelpArticleSummary>({
        columnId: 'title',
        renderHeaderCell: () => 'Title',
        renderCell: (item) => item.title,
      }),
      createTableColumn<HelpArticleSummary>({
        columnId: 'category',
        renderHeaderCell: () => 'Category',
        renderCell: (item) => item.categoryName ?? 'General',
      }),
      createTableColumn<HelpArticleSummary>({
        columnId: 'status',
        renderHeaderCell: () => 'Status',
        renderCell: (item) => (
          <Badge appearance="outline" color={item.isPublished ? 'success' : 'informative'}>
            {item.isPublished ? 'Published' : 'Draft'}
          </Badge>
        ),
      }),
      createTableColumn<HelpArticleSummary>({
        columnId: 'updatedAt',
        renderHeaderCell: () => 'Updated',
        renderCell: (item) => formatDateTime(item.updatedAt),
      }),
    ],
    [],
  );

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div className="flex items-start justify-between gap-4">
        <div>
          <Subtitle2>Help articles</Subtitle2>
          <Text className="text-sm text-neutral-foreground-3">
            Create and publish help articles for the Help Centre drawer.
          </Text>
        </div>
        <Button
          appearance="primary"
          icon={<AddRegular />}
          onClick={() => {
            setSelectedArticle(null);
            setIsCreating(true);
            setIsDrawerOpen(true);
          }}
        >
          New article
        </Button>
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex-1 min-w-0 overflow-auto">
        {isLoading ? (
          <Spinner label="Loading articles..." />
        ) : (
          <DataGrid items={articles} columns={columns} getRowId={(item) => item.id}>
            <DataGridHeader>
              <DataGridRow>
                {({ renderHeaderCell }) => (
                  <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                )}
              </DataGridRow>
            </DataGridHeader>
            <DataGridBody<HelpArticleSummary>>
              {({ item, rowId }) => (
                <DataGridRow<HelpArticleSummary>
                  key={rowId}
                  onClick={() => void openArticle(item.id)}
                  className={selectedArticle?.id === item.id ? 'bg-neutral-background-2' : undefined}
                >
                  {({ renderCell }) => (
                    <DataGridCell>{renderCell(item)}</DataGridCell>
                  )}
                </DataGridRow>
              )}
            </DataGridBody>
          </DataGrid>
        )}
      </div>

      <HelpArticleFormDrawer
        open={isDrawerOpen}
        article={isCreating ? null : selectedArticle}
        existingCategories={existingCategories}
        onClose={closeDrawer}
        onSaved={() => void loadArticles()}
        onDeleted={() => {
          void loadArticles();
        }}
      />
    </div>
  );
}
