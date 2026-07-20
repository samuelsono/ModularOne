import { useEffect, useRef, useState, type FormEvent } from 'react';
import {
  Button,
  DrawerBody,
  DrawerHeader,
  DrawerHeaderTitle,
  Dropdown,
  Field,
  InfoLabel,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  OverlayDrawer,
  Spinner,
  Text,
  Textarea,
} from '@fluentui/react-components';
import { ArrowUploadRegular, AttachRegular, Dismiss24Regular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import {
  createLeaveRequest,
  getLeaveTypes,
  getMyLeaveBalances,
  getWorkingDaysPreview,
} from '@modules/leave/services/leaveService';
import type { LeaveBalance, LeaveType } from '@modules/leave/types/leave';
import { formatDateOnlyForApi, parseDateOnly } from '@platform/utils/dateOnly';
import { DatePicker } from '@fluentui/react-datepicker-compat';

interface LeaveRequestFormProps {
  open: boolean;
  onClose: () => void;
  onSubmitted: () => void;
}

export function LeaveRequestForm({ open, onClose, onSubmitted }: LeaveRequestFormProps) {
  const [leaveTypes, setLeaveTypes] = useState<LeaveType[]>([]);
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [isLoadingTypes, setIsLoadingTypes] = useState(false);
  const [leaveTypeId, setLeaveTypeId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [startDayPortion, setStartDayPortion] = useState('Full');
  const [endDayPortion, setEndDayPortion] = useState('Full');
  const [notes, setNotes] = useState('');
  const [documentFile, setDocumentFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [workingDays, setWorkingDays] = useState<number | null>(null);
  const [isCalculatingDays, setIsCalculatingDays] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setLeaveTypeId('');
      setStartDate('');
      setEndDate('');
      setStartDayPortion('Full');
      setEndDayPortion('Full');
      setNotes('');
      setDocumentFile(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      setWorkingDays(null);
      setError(null);
      return;
    }

    setIsLoadingTypes(true);
    void Promise.all([getLeaveTypes(), getMyLeaveBalances()])
      .then(([types, balanceRows]) => {
        setLeaveTypes(types);
        setBalances(balanceRows);
        if (types.length > 0) {
          setLeaveTypeId(types[0].id);
        }
      })
      .catch((loadError) => {
        setError(loadError instanceof ApiError ? loadError.message : 'Failed to load leave types.');
        setLeaveTypes([]);
        setBalances([]);
      })
      .finally(() => setIsLoadingTypes(false));
  }, [open]);

  useEffect(() => {
    if (!open || !startDate || !endDate) {
      setWorkingDays(null);
      return;
    }

    setIsCalculatingDays(true);
    const timer = window.setTimeout(() => {
      void getWorkingDaysPreview(startDate, endDate, startDayPortion, endDayPortion)
        .then((result) => setWorkingDays(result.workingDays))
        .catch(() => setWorkingDays(null))
        .finally(() => setIsCalculatingDays(false));
    }, 300);

    return () => window.clearTimeout(timer);
  }, [open, startDate, endDate, startDayPortion, endDayPortion]);

  const selectedType = leaveTypes.find((type) => type.id === leaveTypeId);
  const selectedBalance = balances.find((balance) => balance.leaveTypeId === leaveTypeId);
  const isSameDay = Boolean(startDate && endDate && startDate === endDate);
  const showHalfDayOptions = Boolean(selectedType?.allowHalfDay);

  useEffect(() => {
    if (!selectedType?.allowHalfDay) {
      setStartDayPortion('Full');
      setEndDayPortion('Full');
    }
  }, [selectedType?.allowHalfDay, leaveTypeId]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (!leaveTypeId) {
      setError('Please select a leave type.');
      return;
    }

    if (selectedType?.requiresDocument && !documentFile) {
      setError('A supporting document is required for this leave type.');
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      await createLeaveRequest({
        leaveTypeId,
        startDate,
        endDate,
        notes: notes.trim() || null,
        startDayPortion: showHalfDayOptions && startDayPortion !== 'Full' ? startDayPortion : null,
        endDayPortion: showHalfDayOptions && endDayPortion !== 'Full' ? endDayPortion : null,
      }, documentFile);
      onSubmitted();
      onClose();
    } catch (submitError) {
      setError(submitError instanceof ApiError ? submitError.message : 'Failed to submit leave request.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const FieldLabelInfo = ({ text, info }: { text: string, info: string }) => (
      <InfoLabel
         info = {
          <Text className="text-sm text-neutral-foreground-3">
          {info}
        </Text>
         }
      >
        {text}
      </InfoLabel>
  )

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
          Apply for leave
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody>
        {error ? (
          <MessageBar intent="error" className="mb-4">
            <MessageBarBody>{error}</MessageBarBody>
          </MessageBar>
        ) : null}

        {isLoadingTypes ? (
          <Spinner label="Loading leave types..." />
        ) : leaveTypes.length === 0 ? (
          <MessageBar intent="warning">
            <MessageBarBody>No leave types are configured. Contact HR to set up leave policies.</MessageBarBody>
          </MessageBar>
        ) : (
          <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
            <Field label="Leave type" required>
              <Dropdown
                value={selectedType?.name ?? ''}
                selectedOptions={leaveTypeId ? [leaveTypeId] : []}
                onOptionSelect={(_, data) => {
                  if (data.optionValue) {
                    setLeaveTypeId(data.optionValue);
                    setDocumentFile(null);
                    if (fileInputRef.current) {
                      fileInputRef.current.value = '';
                    }
                  }
                }}
              >
                {leaveTypes.map((type) => (
                  <Option key={type.id} value={type.id} text={type.name}>
                    <span className="inline-flex items-center gap-2">
                      <span
                        className="inline-block w-3 h-3 rounded-full shrink-0"
                        style={{ backgroundColor: type.color }}
                      />
                      {type.name}
                    </span>
                  </Option>
                ))}
              </Dropdown>
            </Field>

            {selectedType?.deductsBalance && selectedBalance ? (
              <MessageBar intent="info">
                <MessageBarBody>
                  Remaining balance: {selectedBalance.remaining.toFixed(1)} day(s)
                  {' '}(pending: {selectedBalance.pending.toFixed(1)}, used: {selectedBalance.used.toFixed(1)})
                </MessageBarBody>
              </MessageBar>
            ) : null}

            {selectedType?.minNoticeDays === 0 ? (
              <MessageBar intent="info">
                <MessageBarBody>
                  This leave type allows past dates, so it can be submitted after the leave was taken.
                </MessageBarBody>
              </MessageBar>
            ) : null}

            <Field label="Start date" required>
                <DatePicker
                  placeholder='When Leave starts'
                  value={parseDateOnly(startDate) ?? null}
                  onSelectDate={(date) => setStartDate(date ? formatDateOnlyForApi(date) : '')}
                />
            </Field>

          

            <Field label={<FieldLabelInfo text="End date" info="The end date is inclusive. For example, if you select 1st Jan to 3rd Jan, it will count as 3 days of leave." />} required>
                <DatePicker
                  placeholder='When Leave ends'
                  value={parseDateOnly(endDate) ?? null}
                  onSelectDate={(date) => setEndDate(date ? formatDateOnlyForApi(date) : '')}
                />
            </Field>


            {showHalfDayOptions && startDate && endDate ? (
              isSameDay ? (
                <Field label="Duration">
                  <Dropdown
                    value={startDayPortion === 'Half' ? 'Half day' : 'Full day'}
                    selectedOptions={[startDayPortion === 'Half' ? 'Half' : 'Full']}
                    onOptionSelect={(_, data) => {
                      const value = data.optionValue === 'Half' ? 'Half' : 'Full';
                      setStartDayPortion(value);
                      setEndDayPortion(value);
                    }}
                  >
                    <Option value="Full" text="Full day">Full day</Option>
                    <Option value="Half" text="Half day">Half day</Option>
                  </Dropdown>
                </Field>
              ) : (
                <>
                  <Field label="First day">
                    <Dropdown
                      value={startDayPortion === 'Half' ? 'Half day' : 'Full day'}
                      selectedOptions={[startDayPortion === 'Half' ? 'Half' : 'Full']}
                      onOptionSelect={(_, data) => {
                        setStartDayPortion(data.optionValue === 'Half' ? 'Half' : 'Full');
                      }}
                    >
                      <Option value="Full" text="Full day">Full day</Option>
                      <Option value="Half" text="Half day">Half day</Option>
                    </Dropdown>
                  </Field>
                  <Field label="Last day">
                    <Dropdown
                      value={endDayPortion === 'Half' ? 'Half day' : 'Full day'}
                      selectedOptions={[endDayPortion === 'Half' ? 'Half' : 'Full']}
                      onOptionSelect={(_, data) => {
                        setEndDayPortion(data.optionValue === 'Half' ? 'Half' : 'Full');
                      }}
                    >
                      <Option value="Full" text="Full day">Full day</Option>
                      <Option value="Half" text="Half day">Half day</Option>
                    </Dropdown>
                  </Field>
                </>
              )
            ) : null}

            {startDate && endDate ? (
              <Text className="text-sm text-neutral-foreground-3">
                {isCalculatingDays
                  ? 'Calculating working days...'
                  : workingDays !== null
                    ? `${workingDays} working day(s) (weekends and public holidays excluded)`
                    : 'Unable to calculate working days for this range.'}
              </Text>
            ) : null}

            <Field label="Notes">
              <Textarea
                value={notes}
                maxLength={1024}
                resize="vertical"
                rows={4}
                onChange={(_, data) => setNotes(data.value)}
              />
            </Field>

            {selectedType?.requiresDocument ? (
              <Field
                label="Supporting document"
                required
                hint="PDF, JPG, PNG, HEIC, DOC, or DOCX up to 5 MB"
              >
                <Input
                  ref={fileInputRef}
                  type={"file" as "text"}
                  accept=".pdf,.jpg,.jpeg,.png,.heic,.doc,.docx"
                  className="block w-full text-sm cursor-pointer! file:mr-4 pt-1 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-neutral-fill-stealth-2 file:text-neutral-foreground-1 hover:file:bg-neutral-fill-stealth-3"
                  onChange={(event) => setDocumentFile(event.target.files?.[0] ?? null)}
                  contentBefore={<AttachRegular className='mb-1' />}
                />
                {documentFile ? (
                  <Text className="text-sm text-neutral-foreground-3 mt-1">
                    Selected: {documentFile.name}
                  </Text>
                ) : null}
              </Field>
            ) : null}

            <div className="flex gap-2 pt-2">
              <Button type="button" appearance="secondary" onClick={onClose} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button type="submit" appearance="primary" disabled={isSubmitting}>
                {isSubmitting ? 'Submitting...' : 'Submit request'}
              </Button>
            </div>
          </form>
        )}
      </DrawerBody>
    </OverlayDrawer>
  );
}
