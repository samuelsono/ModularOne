import { useEffect, useMemo, useState, type FormEvent } from 'react';
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
  Textarea,
} from '@fluentui/react-components';
import { DatePicker } from '@fluentui/react-datepicker-compat';
import { AttachRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import {
  createExpenseClaim,
  getExpenseCategories,
  getExpenseSettings,
  submitExpenseClaim,
  updateExpenseClaim,
  uploadExpenseReceipt,
} from '@modules/expense/services/expenseService';
import type { ExpenseCategory, ExpenseClaim } from '@modules/expense/types/expense';
import { formatDateOnlyForApi, parseDateOnly } from '@platform/utils/dateOnly';
import AppTitle from '@platform/ui/AppTitle';

interface ExpenseClaimFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmitted: () => void;
  claim?: ExpenseClaim | null;
}

const currencies = ['ZAR', 'USD', 'EUR', 'GBP', 'JPY', 'AUD', 'CAD', 'CHF', 'CNY', 'SEK', 'NZD'] as const;

function parseAmountInput(value: string): number | undefined {
  const trimmed = value.trim();
  if (!trimmed) {
    return undefined;
  }

  const numeric = Number.parseFloat(trimmed);
  if (!Number.isFinite(numeric) || numeric <= 0) {
    return undefined;
  }

  return Math.round(numeric * 100) / 100;
}

export function ExpenseClaimForm({ open, onOpenChange, onSubmitted, claim = null }: ExpenseClaimFormProps) {
  const isEdit = claim !== null;
  const [categories, setCategories] = useState<ExpenseCategory[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [categoryId, setCategoryId] = useState('');
  const [expenseDate, setExpenseDate] = useState('');
  const [description, setDescription] = useState('');
  const [notes, setNotes] = useState('');
  const [amountInput, setAmountInput] = useState('');
  const [currency, setCurrency] = useState<string>('ZAR');

  const [kilometersTravelled, setKilometersTravelled] = useState('');
  const [travelStartPoint, setTravelStartPoint] = useState('');
  const [travelDestination, setTravelDestination] = useState('');
  const [travelWaypoints, setTravelWaypoints] = useState('');
  const [receiptFile, setReceiptFile] = useState<File | null>(null);
  const [currentMileageRate, setCurrentMileageRate] = useState<number | null>(null);

  const selectedCategory = categories.find((category) => category.id === categoryId);

  useEffect(() => {
    if (!open) {
      setCategoryId('');
      setExpenseDate('');
      setDescription('');
      setNotes('');
      setAmountInput('');
      setCurrency('ZAR');
      setKilometersTravelled('');
      setTravelStartPoint('');
      setTravelDestination('');
      setTravelWaypoints('');
      setReceiptFile(null);
      setCurrentMileageRate(null);
      setError(null);
      return;
    }

    if (claim) {
      setCategoryId(claim.categoryId);
      setExpenseDate(claim.expenseDate);
      setDescription(claim.description);
      setNotes(claim.notes ?? '');
      setAmountInput(String(claim.amount));
      setCurrency(claim.currency || 'ZAR');
      setKilometersTravelled(claim.kilometersTravelled ? String(claim.kilometersTravelled) : '');
      setTravelStartPoint(claim.travelStartPoint ?? '');
      setTravelDestination(claim.travelDestination ?? '');
      setTravelWaypoints((claim.travelWaypoints ?? []).join(', '));
      setReceiptFile(null);
    }

    setIsLoading(true);
    void Promise.all([getExpenseCategories(), getExpenseSettings()])
      .then(([items, settings]) => {
        setCategories(items);
        setCurrentMileageRate(settings.kilometerRate);

        if (!claim && items.length > 0) {
          setCategoryId(items[0].id);
        }
      })
      .catch((loadError) => {
        setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense settings.');
        setCategories([]);
      })
      .finally(() => setIsLoading(false));
  }, [claim, open]);

  const amount = useMemo(() => parseAmountInput(amountInput), [amountInput]);
  const parsedKilometers = useMemo(() => parseAmountInput(kilometersTravelled), [kilometersTravelled]);
  const requiresTravelDetails = Boolean(selectedCategory?.requiresTravelDetails);
  const requiresReceipt = Boolean(selectedCategory?.requiresReceipt);
  const paysByKilometer = Boolean(selectedCategory?.paysByKilometer);

  const canSave = useMemo(
    () => Boolean(
      categoryId
      && expenseDate
      && description.trim()
      && (paysByKilometer ? parsedKilometers !== undefined : amount !== undefined)
      && (!requiresTravelDetails
        || (parsedKilometers !== undefined
          && travelStartPoint.trim().length > 0
          && travelDestination.trim().length > 0)),
    ),
    [
      amount,
      categoryId,
      description,
      expenseDate,
      parsedKilometers,
      paysByKilometer,
      requiresTravelDetails,
      travelDestination,
      travelStartPoint,
    ],
  );

  async function persistClaim(submitAfterSave: boolean) {
    if (!canSave) {
      return;
    }

    if (requiresReceipt && !claim?.hasReceipt && !receiptFile) {
      setError('A receipt is required for this expense category.');
      return;
    }

    setIsSaving(true);
    setError(null);

    const waypoints = travelWaypoints
      .split(',')
      .map((item) => item.trim())
      .filter((item) => item.length > 0);

    const payload = {
      categoryId,
      expenseDate,
      description: description.trim(),
      notes: notes.trim() || null,
      amount: paysByKilometer ? (amount ?? 1) : (amount ?? 0),
      currency: currency.trim() || 'ZAR',
      kilometersTravelled: parsedKilometers ?? null,
      travelStartPoint: travelStartPoint.trim() || null,
      travelDestination: travelDestination.trim() || null,
      travelWaypoints: waypoints.length > 0 ? waypoints : null,
    };

    try {
      const saved = isEdit && claim
        ? await updateExpenseClaim(claim.id, payload)
        : await createExpenseClaim(payload);

      if (receiptFile) {
        await uploadExpenseReceipt(saved.id, receiptFile);
      }

      if (submitAfterSave) {
        await submitExpenseClaim(saved.id);
      }

      onOpenChange(false);
      onSubmitted();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save expense claim.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleSaveDraft(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await persistClaim(false);
  }

  async function handleSaveAndSubmit() {
    await persistClaim(true);
  }

  return (
    <Dialog
      open={open}
      modalType="modal"
      onOpenChange={(_, data) => onOpenChange(data.open)}
    >
      <DialogSurface aria-describedby={undefined}>
        <form onSubmit={handleSaveDraft}>
          <DialogBody>
            <DialogTitle>
              <AppTitle
                title={isEdit ? 'Edit expense claim' : 'New expense claim'}
                subtitle={isEdit
                  ? 'Update your draft claim, then save or submit for approval.'
                  : 'Save as draft or submit for manager and finance approval.'}
              />
            </DialogTitle>

            <DialogContent className="flex flex-col gap-4 pt-5!">
              {error ? (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              ) : null}

              {isLoading ? (
                <Spinner label="Loading expense categories..." />
              ) : (
                <>
                  <div className="flex flex-col gap-3 md:flex-row md:items-end md:gap-3">
                    <Field label="Expense category" required className="min-w-0 flex-1">
                      <Dropdown
                        value={selectedCategory?.name ?? ''}
                        selectedOptions={categoryId ? [categoryId] : []}
                        onOptionSelect={(_, data) => {
                          if (data.optionValue) {
                            setCategoryId(data.optionValue);
                          }
                        }}
                      >
                        {categories.map((category) => (
                          <Option key={category.id} value={category.id} text={category.name}>
                            {category.name}
                          </Option>
                        ))}
                      </Dropdown>
                    </Field>

                    <Field label="Currency" required className="w-full md:w-[120px] shrink-0">
                      <Dropdown
                        value={currency}
                        style={{ minWidth: 120, width: 120 }}
                        selectedOptions={[currency]}
                        onOptionSelect={(_, data) => {
                          if (data.optionValue) {
                            setCurrency(data.optionValue);
                          }
                        }}
                      >
                        {currencies.map((cur) => (
                          <Option key={cur} value={cur} text={cur}>
                            {cur}
                          </Option>
                        ))}
                      </Dropdown>
                    </Field>
                  </div>

                  <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                    <Field label="Expense date" required>
                      <DatePicker
                        placeholder="Select expense date"
                        value={parseDateOnly(expenseDate) ?? null}
                        onSelectDate={(date) => {
                          setExpenseDate(date ? formatDateOnlyForApi(date) : '');
                        }}
                      />
                    </Field>

                    <Field label="Amount" required>
                      <Input
                        type="number"
                        inputMode="decimal"
                        step="0.01"
                        min="0"
                        placeholder="0.00"
                        value={amountInput}
                        onChange={(_, data) => setAmountInput(data.value)}
                        disabled={paysByKilometer}
                      />
                    </Field>
                  </div>

                  {requiresTravelDetails ? (
                    <>
                      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
                        <Field label="Kilometers travelled" required>
                          <Input
                            type="number"
                            inputMode="decimal"
                            step="0.01"
                            min="0"
                            placeholder="0.00"
                            value={kilometersTravelled}
                            onChange={(_, data) => setKilometersTravelled(data.value)}
                          />
                        </Field>

                        <Field label="Starting point" required>
                          <Input
                            value={travelStartPoint}
                            maxLength={256}
                            onChange={(_, data) => setTravelStartPoint(data.value)}
                          />
                        </Field>
                      </div>

                      <Field label="Destination" required>
                        <Input
                          value={travelDestination}
                          maxLength={256}
                          onChange={(_, data) => setTravelDestination(data.value)}
                        />
                      </Field>

                      <Field label="Waypoints (comma separated)">
                        <Textarea
                          value={travelWaypoints}
                          onChange={(_, data) => setTravelWaypoints(data.value)}
                          resize="vertical"
                        />
                      </Field>

                      {paysByKilometer && currentMileageRate !== null ? (
                        <MessageBar intent="info">
                          <MessageBarBody>
                            Mileage rate: {currentMileageRate.toFixed(2)} ZAR/km. Amount is calculated from kilometers.
                          </MessageBarBody>
                        </MessageBar>
                      ) : null}
                    </>
                  ) : null}

                  <Field label="Description" required>
                    <Textarea
                      value={description}
                      onChange={(_, data) => setDescription(data.value)}
                      resize="vertical"
                    />
                  </Field>

                  <Field label="Notes">
                    <Textarea
                      value={notes}
                      onChange={(_, data) => setNotes(data.value)}
                      resize="vertical"
                    />
                  </Field>

                  {requiresReceipt ? (
                    <Field
                      label="Receipt"
                      required
                      hint="PDF, JPG, PNG, HEIC, DOC, or DOCX up to 5 MB"
                    >
                      <Input
                        type={"file" as "text"}
                        accept=".pdf,.jpg,.jpeg,.png,.heic,.doc,.docx"
                        className="block w-full text-sm cursor-pointer! file:mr-4 pt-1 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-neutral-fill-stealth-2 file:text-neutral-foreground-1 hover:file:bg-neutral-fill-stealth-3"
                        onChange={(event) => setReceiptFile(event.target.files?.[0] ?? null)}
                        contentBefore={<AttachRegular className="mb-1" />}
                      />
                      {claim?.hasReceipt && !receiptFile ? (
                        <MessageBar intent="info">
                          <MessageBarBody>
                            Existing receipt: {claim.receiptFileName ?? 'attached'}
                          </MessageBarBody>
                        </MessageBar>
                      ) : null}
                    </Field>
                  ) : null}
                </>
              )}
            </DialogContent>

            <DialogActions className="flex! items-center! justify-end! gap-2! py-3!">
              <Button type="button" appearance="secondary" onClick={() => onOpenChange(false)} disabled={isSaving}>
                Close
              </Button>
              <Button type="submit" appearance="secondary" disabled={!canSave || isLoading || isSaving}>
                {isSaving ? 'Saving...' : 'Save draft'}
              </Button>
              <Button
                type="button"
                appearance="primary"
                className="text-nowrap"
                disabled={!canSave || isLoading || isSaving}
                onClick={() => void handleSaveAndSubmit()}
              >
                {isSaving ? 'Submitting...' : 'Submit for approval'}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
