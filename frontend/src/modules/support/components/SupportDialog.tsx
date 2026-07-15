import { useEffect, useMemo, useState, type FormEvent } from 'react';
import type { JSXElement } from '@fluentui/react-components';
import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Text,
  Textarea,
} from '@fluentui/react-components';
import {
  BugRegular,
  CheckmarkCircleRegular,
  ThumbLikeRegular,
  TicketDiagonalRegular,
} from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import { getCategories, submitTicket } from '@modules/support/services/supportService';
import type {
  BugSeverity,
  TicketCategory,
  TicketPriority,
  TicketType,
} from '@modules/support/types/support';

const PRIORITIES: TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];
const BUG_SEVERITIES: BugSeverity[] = ['Minor', 'Major', 'Critical', 'Blocker'];

const MODE_CONFIG: Record<
  TicketType,
  { title: string; icon: typeof TicketDiagonalRegular; successMessage: string }
> = {
  Ticket: {
    title: 'Create Support Ticket',
    icon: TicketDiagonalRegular,
    successMessage: 'Your ticket has been submitted! We\'ll get back to you shortly.',
  },
  Feedback: {
    title: 'Share Feedback',
    icon: ThumbLikeRegular,
    successMessage: 'Thank you for your feedback!',
  },
  BugReport: {
    title: 'Report a Bug',
    icon: BugRegular,
    successMessage: 'Your bug report has been submitted. Thank you for helping us improve.',
  },
};

interface SupportDialogProps {
  open: boolean;
  onClose: () => void;
  initialMode: TicketType;
}

export function SupportDialog({
  open,
  onClose,
  initialMode,
}: SupportDialogProps): JSXElement {
  const config = MODE_CONFIG[initialMode];
  const ModeIcon = config.icon;

  const [categories, setCategories] = useState<TicketCategory[]>([]);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);
  const [categoryId, setCategoryId] = useState('');
  const [subject, setSubject] = useState('');
  const [priority, setPriority] = useState<TicketPriority>('Medium');
  const [description, setDescription] = useState('');
  const [satisfactionRating, setSatisfactionRating] = useState<number | null>(null);
  const [bugSeverity, setBugSeverity] = useState<BugSeverity>('Major');
  const [stepsToReproduce, setStepsToReproduce] = useState('');
  const [expectedBehavior, setExpectedBehavior] = useState('');
  const [actualBehavior, setActualBehavior] = useState('');
  const [browserOrEnvironment, setBrowserOrEnvironment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!open) {
      return;
    }

    setIsLoadingCategories(true);
    setError(null);
    setSubmitted(false);
    setFieldErrors({});

    void getCategories(initialMode)
      .then((items) => {
        setCategories(items);
        setCategoryId(items[0]?.id ?? '');
      })
      .catch(() => {
        setCategories([]);
        setCategoryId('');
        setError('Unable to load categories.');
      })
      .finally(() => setIsLoadingCategories(false));
  }, [open, initialMode]);

  useEffect(() => {
    if (!open) {
      setSubject('');
      setPriority('Medium');
      setDescription('');
      setSatisfactionRating(null);
      setBugSeverity('Major');
      setStepsToReproduce('');
      setExpectedBehavior('');
      setActualBehavior('');
      setBrowserOrEnvironment('');
      setSubmitted(false);
      setError(null);
      setFieldErrors({});
    }
  }, [open]);

  const selectedCategoryLabel = useMemo(
    () => categories.find((category) => category.id === categoryId)?.label ?? '',
    [categories, categoryId],
  );

  function validateForm(): boolean {
    const errors: Record<string, string> = {};

    if (!categoryId) {
      errors.categoryId = 'Category is required.';
    }

    if (!subject.trim()) {
      errors.subject = 'Subject is required.';
    }

    if (!description.trim()) {
      errors.description = 'Description is required.';
    } else if (description.trim().length < 20) {
      errors.description = 'Description must be at least 20 characters.';
    }

    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await submitTicket({
        type: initialMode,
        subject: subject.trim(),
        description: description.trim(),
        categoryId,
        priority,
        bugSeverity: initialMode === 'BugReport' ? bugSeverity : undefined,
        stepsToReproduce: initialMode === 'BugReport' ? stepsToReproduce.trim() || undefined : undefined,
        expectedBehavior: initialMode === 'BugReport' ? expectedBehavior.trim() || undefined : undefined,
        actualBehavior: initialMode === 'BugReport' ? actualBehavior.trim() || undefined : undefined,
        browserOrEnvironment: initialMode === 'BugReport'
          ? browserOrEnvironment.trim() || undefined
          : undefined,
        satisfactionRating: initialMode === 'Feedback' ? satisfactionRating ?? undefined : undefined,
      });
      setSubmitted(true);
      window.setTimeout(() => onClose(), 2000);
    } catch (submitError) {
      const message = submitError instanceof ApiError
        ? submitError.message
        : 'Failed to submit. Please try again.';
      setError(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className="flex! w-[560px]! max-w-[95vw]! flex-col!">
        {submitted ? (
          <>
            <DialogBody>
              <DialogTitle>{config.title}</DialogTitle>
              <DialogContent className="flex flex-col items-center gap-3 py-8 text-center">
                <CheckmarkCircleRegular className="size-12 text-green-600" />
                <Text weight="semibold">{config.successMessage}</Text>
              </DialogContent>
            </DialogBody>
            <DialogActions>
              <Button appearance="primary" onClick={onClose}>Close</Button>
            </DialogActions>
          </>
        ) : (
          <form onSubmit={(event) => void handleSubmit(event)}>
            <DialogBody>
              <DialogTitle className="flex items-center gap-2">
                <ModeIcon className="size-5" />
                {config.title}
              </DialogTitle>
              <DialogContent className="flex flex-col gap-3 pt-2">
                {error ? (
                  <MessageBar intent="error">
                    <MessageBarBody>{error}</MessageBarBody>
                  </MessageBar>
                ) : null}

                {isLoadingCategories ? (
                  <Spinner label="Loading categories..." />
                ) : (
                  <>
                    <Field
                      label="Category"
                      required
                      validationState={fieldErrors.categoryId ? 'error' : 'none'}
                      validationMessage={fieldErrors.categoryId}
                    >
                      <Dropdown
                        value={selectedCategoryLabel}
                        selectedOptions={categoryId ? [categoryId] : []}
                        onOptionSelect={(_, data) => setCategoryId(data.optionValue ?? '')}
                      >
                        {categories.map((category) => (
                          <Option key={category.id} value={category.id}>
                            {category.label}
                          </Option>
                        ))}
                      </Dropdown>
                    </Field>

                    <Field
                      label="Subject"
                      required
                      validationState={fieldErrors.subject ? 'error' : 'none'}
                      validationMessage={fieldErrors.subject}
                    >
                      <Input
                        value={subject}
                        maxLength={200}
                        onChange={(_, data) => setSubject(data.value)}
                      />
                    </Field>

                    <Field label="Priority">
                      <Dropdown
                        value={priority}
                        selectedOptions={[priority]}
                        onOptionSelect={(_, data) => {
                          const next = data.optionValue as TicketPriority | undefined;
                          if (next) {
                            setPriority(next);
                          }
                        }}
                      >
                        {PRIORITIES.map((item) => (
                          <Option key={item} value={item}>{item}</Option>
                        ))}
                      </Dropdown>
                    </Field>

                    <Field
                      label="Description"
                      required
                      validationState={fieldErrors.description ? 'error' : 'none'}
                      validationMessage={fieldErrors.description}
                    >
                      <Textarea
                        value={description}
                        maxLength={4000}
                        resize="vertical"
                        rows={4}
                        onChange={(_, data) => setDescription(data.value)}
                      />
                    </Field>

                    {initialMode === 'Feedback' ? (
                      <Field label="How satisfied are you?">
                        <div className="flex gap-1">
                          {[1, 2, 3, 4, 5].map((rating) => (
                            <Button
                              key={rating}
                              type="button"
                              appearance="subtle"
                              aria-label={`${rating} star${rating === 1 ? '' : 's'}`}
                              onClick={() => setSatisfactionRating(rating)}
                            >
                              <span className={rating <= (satisfactionRating ?? 0) ? 'text-amber-500' : 'text-neutral-400'}>
                                ★
                              </span>
                            </Button>
                          ))}
                        </div>
                      </Field>
                    ) : null}

                    {initialMode === 'BugReport' ? (
                      <>
                        <Field label="Bug severity">
                          <Dropdown
                            value={bugSeverity}
                            selectedOptions={[bugSeverity]}
                            onOptionSelect={(_, data) => {
                              const next = data.optionValue as BugSeverity | undefined;
                              if (next) {
                                setBugSeverity(next);
                              }
                            }}
                          >
                            {BUG_SEVERITIES.map((item) => (
                              <Option key={item} value={item}>{item}</Option>
                            ))}
                          </Dropdown>
                        </Field>

                        <Field label="Steps to reproduce">
                          <Textarea
                            value={stepsToReproduce}
                            maxLength={4000}
                            resize="vertical"
                            rows={3}
                            onChange={(_, data) => setStepsToReproduce(data.value)}
                          />
                        </Field>

                        <Field label="Expected behavior">
                          <Input
                            value={expectedBehavior}
                            maxLength={1000}
                            onChange={(_, data) => setExpectedBehavior(data.value)}
                          />
                        </Field>

                        <Field label="Actual behavior">
                          <Input
                            value={actualBehavior}
                            maxLength={1000}
                            onChange={(_, data) => setActualBehavior(data.value)}
                          />
                        </Field>

                        <Field label="Browser / environment">
                          <Input
                            value={browserOrEnvironment}
                            maxLength={512}
                            placeholder="e.g. Chrome 120 on macOS"
                            onChange={(_, data) => setBrowserOrEnvironment(data.value)}
                          />
                        </Field>
                      </>
                    ) : null}
                  </>
                )}
              </DialogContent>
            </DialogBody>
            <DialogActions className='mt-4'>
              <Button type="button" appearance="secondary" onClick={onClose} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button type="submit" appearance="primary" disabled={isSubmitting || isLoadingCategories}>
                {isSubmitting ? 'Submitting...' : 'Submit'}
              </Button>
            </DialogActions>
          </form>
        )}
      </DialogSurface>
    </Dialog>
  );
}
