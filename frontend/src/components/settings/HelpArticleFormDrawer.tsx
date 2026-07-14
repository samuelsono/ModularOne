import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Badge,
  Button,
  Combobox,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  OverlayDrawer,
  Switch,
  Tab,
  TabList,
  Textarea,
  type SelectTabData,
  type SelectTabEvent,
  type TabValue,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import ReactMarkdown from 'react-markdown';
import { ApiError } from '../../services/apiClient';
import {
  createArticle,
  deleteArticle,
  getCategories,
  updateArticle,
} from '../../services/helpService';
import type { HelpArticleDetail, SaveHelpArticleRequest } from '../../types/help';
import { ConfirmAction } from '../ConfirmAction';

const DEFAULT_HELP_CATEGORIES = [
  'Getting Started',
  'Vehicles',
  'Drivers',
  'Reports & Dashboards',
  'Account & Access',
  'Integrations',
  'Notifications',
  'General',
];

interface HelpArticleFormDrawerProps {
  open: boolean;
  article: HelpArticleDetail | null;
  existingCategories: string[];
  onClose: () => void;
  onSaved: () => void;
  onDeleted: () => void;
}

function parseTagsInput(value: string): string[] {
  return value
    .split(',')
    .map((tag) => tag.trim())
    .filter(Boolean);
}

export function HelpArticleFormDrawer({
  open,
  article,
  existingCategories,
  onClose,
  onSaved,
  onDeleted,
}: HelpArticleFormDrawerProps) {
  const isEdit = article !== null;
  const [title, setTitle] = useState('');
  const [categoryName, setCategoryName] = useState('');
  const [tagsInput, setTagsInput] = useState('');
  const [body, setBody] = useState('');
  const [isPublished, setIsPublished] = useState(false);
  const [activeTab, setActiveTab] = useState<TabValue>('edit');
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [fetchedCategories, setFetchedCategories] = useState<string[]>([]);

  useEffect(() => {
    setTitle(article?.title ?? '');
    setCategoryName(article?.categoryName ?? '');
    setTagsInput(article?.tags.join(', ') ?? '');
    setBody(article?.body ?? '');
    setIsPublished(article?.isPublished ?? false);
    setActiveTab('edit');
    setError(null);
  }, [article, open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    void getCategories()
      .then(setFetchedCategories)
      .catch(() => setFetchedCategories([]));
  }, [open]);

  const categorySuggestions = useMemo(
    () => [
      ...new Set([
        ...DEFAULT_HELP_CATEGORIES,
        ...existingCategories,
        ...fetchedCategories,
      ]),
    ].sort((left, right) => left.localeCompare(right)),
    [existingCategories, fetchedCategories],
  );

  const categoryOptions = useMemo(
    () => categorySuggestions.filter((item) =>
      !categoryName || item.toLowerCase().includes(categoryName.toLowerCase()),
    ),
    [categorySuggestions, categoryName],
  );

  function buildPayload(publish: boolean): SaveHelpArticleRequest {
    return {
      title: title.trim(),
      body: body.trim(),
      categoryName: categoryName.trim() || null,
      tags: parseTagsInput(tagsInput),
      isPublished: publish,
    };
  }

  async function handleSave(publish: boolean) {
    if (!title.trim() || !body.trim()) {
      setError('Title and body are required.');
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      const payload = buildPayload(publish);
      if (isEdit && article) {
        await updateArticle(article.id, payload);
      } else {
        await createArticle(payload);
      }
      onSaved();
      onClose();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save article.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete() {
    if (!article) {
      return;
    }

    try {
      await deleteArticle(article.id);
      onDeleted();
      onClose();
    } catch (deleteError) {
      setError(deleteError instanceof ApiError ? deleteError.message : 'Failed to delete article.');
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    void handleSave(isPublished);
  }

  return (
    <OverlayDrawer
      open={open}
      position="end"
      size="medium"
      onOpenChange={(_, data) => !data.open && onClose()}
    >
      <DrawerHeader>
        <DrawerHeaderTitle
          action={(
            <Button
              appearance="subtle"
              aria-label="Close"
              icon={<Dismiss24Regular />}
              onClick={onClose}
            />
          )}
        >
          {isEdit ? 'Edit article' : 'New article'}
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody className="flex flex-col gap-4">
        {error ? (
          <MessageBar intent="error">
            <MessageBarBody>{error}</MessageBarBody>
          </MessageBar>
        ) : null}

        <form className="flex flex-col gap-3" onSubmit={handleSubmit}>
          <Field label="Title" required>
            <Input
              value={title}
              maxLength={200}
              onChange={(_, data) => setTitle(data.value)}
            />
          </Field>

          <Field label="Category">
            <Combobox
              freeform
              placeholder="Select or type a category"
              value={categoryName}
              onChange={(event) => setCategoryName(event.target.value)}
              onOptionSelect={(_, data) => setCategoryName(data.optionText ?? '')}
            >
              {categoryOptions.map((item) => (
                <Option key={item} value={item}>{item}</Option>
              ))}
            </Combobox>
          </Field>

          <Field label="Tags" hint="Comma-separated">
            <Input
              value={tagsInput}
              placeholder="getting-started, vehicles"
              onChange={(_, data) => setTagsInput(data.value)}
            />
          </Field>

          {parseTagsInput(tagsInput).length > 0 ? (
            <div className="flex flex-wrap gap-1">
              {parseTagsInput(tagsInput).map((tag) => (
                <Badge key={tag} appearance="outline" size="small">{tag}</Badge>
              ))}
            </div>
          ) : null}

          <TabList
            selectedValue={activeTab}
            onTabSelect={(_: SelectTabEvent, data: SelectTabData) => setActiveTab(data.value)}
          >
            <Tab value="edit">Edit</Tab>
            <Tab value="preview">Preview</Tab>
          </TabList>

          {activeTab === 'edit' ? (
            <Field label="Body (Markdown)" required>
              <Textarea
                value={body}
                maxLength={50000}
                resize="vertical"
                rows={14}
                onChange={(_, data) => setBody(data.value)}
              />
            </Field>
          ) : (
            <div className="rounded border border-neutral-stroke-2 p-3 min-h-[280px] text-sm leading-relaxed">
              <ReactMarkdown>{body || '*Nothing to preview yet.*'}</ReactMarkdown>
            </div>
          )}

          <Field label="Published">
            <Switch checked={isPublished} onChange={(_, data) => setIsPublished(Boolean(data.checked))} />
          </Field>

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              appearance="secondary"
              disabled={isSaving}
              onClick={() => void handleSave(false)}
            >
              Save draft
            </Button>
            <Button
              type="button"
              appearance="primary"
              disabled={isSaving}
              onClick={() => void handleSave(true)}
            >
              {isSaving ? 'Saving...' : 'Publish'}
            </Button>
            {isEdit ? (
              <Button
                type="button"
                appearance="secondary"
                disabled={isSaving}
                onClick={() => setDeleteOpen(true)}
              >
                Delete
              </Button>
            ) : null}
          </div>
        </form>
      </DrawerBody>

      <ConfirmAction
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title="Delete article"
        message="This will permanently remove the help article"
        actionName="Delete"
        destructive
        onAction={() => void handleDelete()}
      />
    </OverlayDrawer>
  );
}
