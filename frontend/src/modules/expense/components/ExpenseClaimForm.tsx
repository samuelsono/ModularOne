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

import { ApiError } from '@platform/api/apiClient';

import {

  createExpenseClaim,

  getExpenseCategories,

  submitExpenseClaim,

  updateExpenseClaim,

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



  const selectedCategory = categories.find((category) => category.id === categoryId);



  useEffect(() => {

    if (!open) {

      setCategoryId('');

      setExpenseDate('');

      setDescription('');

      setNotes('');

      setAmountInput('');

      setCurrency('ZAR');

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

    }



    setIsLoading(true);

    void getExpenseCategories()

      .then((items) => {

        setCategories(items);

        if (!claim && items.length > 0) {

          setCategoryId(items[0].id);

        }

      })

      .catch((loadError) => {

        setError(loadError instanceof ApiError ? loadError.message : 'Failed to load expense categories.');

        setCategories([]);

      })

      .finally(() => setIsLoading(false));

  }, [claim, open]);



  const amount = useMemo(() => parseAmountInput(amountInput), [amountInput]);



  const canSave = useMemo(

    () => Boolean(

      categoryId

      && expenseDate

      && description.trim()

      && amount !== undefined,

    ),

    [amount, categoryId, description, expenseDate],

  );



  async function persistClaim(submitAfterSave: boolean) {

    if (!canSave || amount === undefined) {

      return;

    }



    setIsSaving(true);

    setError(null);



    const payload = {

      categoryId,

      expenseDate,

      description: description.trim(),

      notes: notes.trim() || null,

      amount,

      currency: currency.trim() || 'ZAR',

    };



    try {

      const saved = isEdit && claim

        ? await updateExpenseClaim(claim.id, payload)

        : await createExpenseClaim(payload);



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

                      />

                    </Field>

                  </div>



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

                </>

              )}

            </DialogContent>

            <DialogActions>

              <Button type="button" appearance="secondary" onClick={() => onOpenChange(false)} disabled={isSaving}>

                Close

              </Button>

              <Button type="submit" appearance="secondary" disabled={!canSave || isLoading || isSaving}>

                {isSaving ? 'Saving...' : 'Save draft'}

              </Button>

              <Button

                type="button"

                appearance="primary"

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


