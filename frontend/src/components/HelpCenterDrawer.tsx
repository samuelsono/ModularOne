import { useEffect, useMemo, useState } from 'react';
import type { JSXElement } from '@fluentui/react-components';
import {
  Badge,
  Button,
  Dropdown,
  Field,
  Option,
  OverlayDrawer,
  SearchBox,
  Skeleton,
  SkeletonItem,
  Text,
  makeStyles,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
} from '@fluentui/react-components';
import {
  ArrowLeftRegular,
  ChevronRightRegular,
  Dismiss24Regular,
  QuestionCircleRegular,
} from '@fluentui/react-icons';
import ReactMarkdown from 'react-markdown';
import { useHelpDrawer } from '../context/HelpDrawerContext';
import { getArticle, getArticles, getCategories } from '../services/helpService';
import type { HelpArticleDetail, HelpArticleSummary } from '../types/help';
import { theme } from '../theme';

const useStyles = makeStyles({
  icon18: { fontSize: '18px' },
  markdown: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    lineHeight: '1.6',
    '& h1, & h2, & h3': { fontWeight: 600, marginTop: '8px' },
    '& ul, & ol': { paddingLeft: '20px' },
    '& code': {
      backgroundColor: '#f3f4f6',
      borderRadius: '4px',
      padding: '2px 6px',
      fontFamily: 'monospace',
    },
    '& pre': {
      backgroundColor: '#f3f4f6',
      borderRadius: '8px',
      padding: '12px',
      overflowX: 'auto',
    },
  },
});

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

function groupArticlesByCategory(articles: HelpArticleSummary[]) {
  const groups = new Map<string, HelpArticleSummary[]>();

  for (const article of articles) {
    const category = article.categoryName ?? 'General';
    const existing = groups.get(category) ?? [];
    existing.push(article);
    groups.set(category, existing);
  }

  return [...groups.entries()].sort(([left], [right]) => left.localeCompare(right));
}

export function HelpCenterDrawer(): JSXElement {
  const styles = useStyles();
  const { open, setOpen } = useHelpDrawer();
  const [selectedArticleId, setSelectedArticleId] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [category, setCategory] = useState<string>('');
  const [categories, setCategories] = useState<string[]>([]);
  const [articles, setArticles] = useState<HelpArticleSummary[]>([]);
  const [selectedArticle, setSelectedArticle] = useState<HelpArticleDetail | null>(null);
  const [isLoadingList, setIsLoadingList] = useState(false);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(search), 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    if (!open) {
      setSelectedArticleId(null);
      setSelectedArticle(null);
      setSearch('');
      setDebouncedSearch('');
      setCategory('');
      setError(null);
      return;
    }

    void getCategories()
      .then(setCategories)
      .catch(() => setCategories([]));
  }, [open]);

  useEffect(() => {
    if (!open || selectedArticleId) {
      return;
    }

    setIsLoadingList(true);
    setError(null);

    void getArticles(debouncedSearch, category || undefined)
      .then(setArticles)
      .catch(() => {
        setArticles([]);
        setError('Unable to load help articles.');
      })
      .finally(() => setIsLoadingList(false));
  }, [open, debouncedSearch, category, selectedArticleId]);

  useEffect(() => {
    if (!selectedArticleId) {
      setSelectedArticle(null);
      return;
    }

    setIsLoadingDetail(true);
    setError(null);

    void getArticle(selectedArticleId)
      .then(setSelectedArticle)
      .catch(() => {
        setSelectedArticle(null);
        setError('Unable to load this article.');
      })
      .finally(() => setIsLoadingDetail(false));
  }, [selectedArticleId]);

  const groupedArticles = useMemo(() => groupArticlesByCategory(articles), [articles]);

  return (
    <>
      <Button
        icon={<QuestionCircleRegular className={styles.icon18} />}
        appearance="transparent"
        aria-label="Help centre"
        onClick={() => setOpen(true)}
        style={{ color: theme.colorBrandBackgroundInverted}}
      />

      <OverlayDrawer
        open={open}
        position="end"
        size="medium"
        onOpenChange={(_, data) => setOpen(data.open)}
      >
        <DrawerHeader>
          <DrawerHeaderTitle
            action={(
              <Button
                appearance="subtle"
                aria-label="Close"
                icon={<Dismiss24Regular />}
                onClick={() => setOpen(false)}
              />
            )}
          >
            {selectedArticleId ? (
              <div className="flex items-center gap-2">
                <Button
                  appearance="subtle"
                  icon={<ArrowLeftRegular />}
                  aria-label="Back to articles"
                  onClick={() => setSelectedArticleId(null)}
                />
                <span>{selectedArticle?.title ?? 'Article'}</span>
              </div>
            ) : (
              'Help Centre'
            )}
          </DrawerHeaderTitle>
        </DrawerHeader>

        <DrawerBody className="flex flex-col gap-4">
          {!selectedArticleId ? (
            <>
              <SearchBox
                placeholder="Search articles..."
                value={search}
                onChange={(_, data) => setSearch(data.value)}
              />

              <Field label="Category">
                <Dropdown
                  value={category || 'All categories'}
                  selectedOptions={category ? [category] : ['']}
                  onOptionSelect={(_, data) => setCategory(data.optionValue ?? '')}
                >
                  <Option value="">All categories</Option>
                  {categories.map((item) => (
                    <Option key={item} value={item}>{item}</Option>
                  ))}
                </Dropdown>
              </Field>

              {error ? <Text className="text-sm text-red-600">{error}</Text> : null}

              {isLoadingList ? (
                <div className="flex flex-col gap-2">
                  {Array.from({ length: 5 }).map((_, index) => (
                    <Skeleton key={index}>
                      <SkeletonItem size={16} />
                    </Skeleton>
                  ))}
                </div>
              ) : groupedArticles.length === 0 ? (
                <div className="text-center py-12">
                  <Text weight="semibold">No articles found</Text>
                  <Text className="text-sm text-neutral-foreground-3">
                    Try a different search or category.
                  </Text>
                </div>
              ) : (
                groupedArticles.map(([categoryName, items]) => (
                  <div key={categoryName} className="flex flex-col gap-1">
                    <Text weight="semibold" className="text-sm">{categoryName}</Text>
                    {items.map((article) => (
                      <button
                        key={article.id}
                        type="button"
                        className="flex items-center justify-between px-2 py-2 rounded hover:bg-neutral-background-2 text-left"
                        onClick={() => setSelectedArticleId(article.id)}
                      >
                        <span className="text-sm">{article.title}</span>
                        <ChevronRightRegular className="size-4 shrink-0" />
                      </button>
                    ))}
                  </div>
                ))
              )}
            </>
          ) : isLoadingDetail ? (
            <Skeleton>
              <SkeletonItem size={24} />
              <SkeletonItem size={16} />
              <SkeletonItem size={128} />
            </Skeleton>
          ) : selectedArticle ? (
            <>
              <div className="flex flex-wrap items-center gap-2 text-sm text-neutral-foreground-3">
                <span>{selectedArticle.categoryName ?? 'General'}</span>
                <span>•</span>
                <span>Updated {formatDate(selectedArticle.updatedAt)}</span>
              </div>

              {selectedArticle.tags.length > 0 ? (
                <div className="flex flex-wrap gap-1">
                  {selectedArticle.tags.map((tag) => (
                    <Badge key={tag} appearance="outline" size="small">{tag}</Badge>
                  ))}
                </div>
              ) : null}

              <div className={styles.markdown}>
                <ReactMarkdown>{selectedArticle.body}</ReactMarkdown>
              </div>
            </>
          ) : (
            <Text className="text-sm text-red-600">{error ?? 'Article not found.'}</Text>
          )}
        </DrawerBody>
      </OverlayDrawer>
    </>
  );
}
