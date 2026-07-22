import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import {
  Button,
  Combobox,
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
import { AttachRegular, Dismiss24Regular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import {
  createLeaveRequest,
  getLeaveTypes,
  getMyLeaveBalances,
  getUserLeaveBalances,
  getWorkingDaysPreview,
} from '@modules/leave/services/leaveService';
import type { LeaveBalance, LeaveType } from '@modules/leave/types/leave';
import { formatDateOnlyForApi, parseDateOnly } from '@platform/utils/dateOnly';
import { DatePicker } from '@fluentui/react-datepicker-compat';
import { usePermissions } from '@platform/permissions/usePermissions';
import { getUsers } from '@modules/users/services/userService';
import type { UserListItem } from '@modules/users/types/user';

interface LeaveRequestFormProps {
  open: boolean;
  onClose: () => void;
  onSubmitted: () => void;
  /** Prefill start date (yyyy-MM-dd) when opening the form. */
  initialStartDate?: string | null;
  /** Prefill end date (yyyy-MM-dd). Defaults to initialStartDate when omitted. */
  initialEndDate?: string | null;
}

export function LeaveRequestForm({
  open,
  onClose,
  onSubmitted,
  initialStartDate = null,
  initialEndDate = null,
}: LeaveRequestFormProps) {
  const { user, isAdmin, isHr } = usePermissions();
  const canCreateOnBehalf = isAdmin || isHr;

  const [employees, setEmployees] = useState<UserListItem[]>([]);
  const [employeeQuery, setEmployeeQuery] = useState('');
  const [onBehalfOfUserId, setOnBehalfOfUserId] = useState<string | null>(null);
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

  const selectedEmployee = useMemo(
    () => employees.find((item) => item.id === onBehalfOfUserId) ?? null,
    [employees, onBehalfOfUserId],
  );

  const filteredEmployees = useMemo(() => {
    const active = employees.filter((item) => item.isActive);
    const query = employeeQuery.trim().toLowerCase();
    if (!query) {
      return active;
    }

    return active.filter((item) => {
      const label = `${item.displayName ?? ''} ${item.email ?? ''} ${item.username ?? ''}`.toLowerCase();
      return label.includes(query);
    });
  }, [employeeQuery, employees]);

  const loadTypesAndBalances = useCallback(async (targetUserId: string | null) => {
    setIsLoadingTypes(true);
    setError(null);
    try {
      const forUserId = targetUserId && targetUserId !== user?.id ? targetUserId : null;
      const [types, balanceRows] = await Promise.all([
        getLeaveTypes(forUserId),
        forUserId ? getUserLeaveBalances(forUserId) : getMyLeaveBalances(),
      ]);
      setLeaveTypes(types);
      setBalances(balanceRows);
      setLeaveTypeId((current) => {
        if (current && types.some((type) => type.id === current)) {
          return current;
        }
        return types[0]?.id ?? '';
      });
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load leave types.');
      setLeaveTypes([]);
      setBalances([]);
      setLeaveTypeId('');
    } finally {
      setIsLoadingTypes(false);
    }
  }, [user?.id]);

  useEffect(() => {
    if (!open) {
      setOnBehalfOfUserId(null);
      setEmployeeQuery('');
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
      setEmployees([]);
      return;
    }

    const selfId = user?.id ?? null;
    setOnBehalfOfUserId(selfId);
    const prefillStart = initialStartDate?.trim() || '';
    const prefillEnd = initialEndDate?.trim() || prefillStart;
    setStartDate(prefillStart);
    setEndDate(prefillEnd);

    if (canCreateOnBehalf) {
      void getUsers()
        .then((response) => setEmployees(response.items ?? []))
        .catch(() => setEmployees([]));
    }
  }, [open, canCreateOnBehalf, user?.id, initialStartDate, initialEndDate]);

  useEffect(() => {
    if (!open) {
      return;
    }

    void loadTypesAndBalances(canCreateOnBehalf ? onBehalfOfUserId : null);
  }, [open, canCreateOnBehalf, onBehalfOfUserId, loadTypesAndBalances]);

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
  const isOnBehalf = Boolean(
    canCreateOnBehalf
    && onBehalfOfUserId
    && onBehalfOfUserId !== user?.id,
  );

  useEffect(() => {
    if (!selectedType?.allowHalfDay) {
      setStartDayPortion('Full');
      setEndDayPortion('Full');
    }
  }, [selectedType?.allowHalfDay, leaveTypeId]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (canCreateOnBehalf && !onBehalfOfUserId && !user?.id) {
      setError('Please select an employee.');
      return;
    }

    if (!leaveTypeId || !selectedType) {
      setError('Please select a leave type.');
      return;
    }

    if (!startDate || !endDate) {
      setError('Please select start and end dates.');
      return;
    }

    if (endDate < startDate) {
      setError('Leave end date must be on or after the start date.');
      return;
    }

    if (isCalculatingDays) {
      setError('Please wait for working days to finish calculating.');
      return;
    }

    if (workingDays === null) {
      setError('Unable to calculate working days for this range. Check the dates and try again.');
      return;
    }

    if (workingDays <= 0) {
      setError('The selected date range contains no working days (weekends and public holidays are excluded).');
      return;
    }

    if (selectedType.minNoticeDays > 0) {
      const earliest = new Date();
      earliest.setHours(0, 0, 0, 0);
      earliest.setDate(earliest.getDate() + selectedType.minNoticeDays);
      const earliestKey = formatDateOnlyForApi(earliest);
      if (startDate < earliestKey) {
        setError(`This leave type requires at least ${selectedType.minNoticeDays} day(s) notice (earliest start: ${earliestKey}).`);
        return;
      }
    }

    if (selectedType.maxConsecutiveDays != null && workingDays > selectedType.maxConsecutiveDays) {
      setError(`This leave type allows at most ${selectedType.maxConsecutiveDays} consecutive working day(s).`);
      return;
    }

    if (selectedType.deductsBalance && selectedBalance && selectedBalance.remaining < workingDays) {
      setError(
        `Insufficient leave balance. Remaining: ${selectedBalance.remaining.toFixed(1)}, requested: ${workingDays.toFixed(1)}.`,
      );
      return;
    }

    if (selectedType.requiresDocument && !documentFile) {
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
        onBehalfOfUserId: isOnBehalf ? onBehalfOfUserId : null,
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
  );

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
          {canCreateOnBehalf ? 'Create leave application' : 'Apply for leave'}
        </DrawerHeaderTitle>
      </DrawerHeader>

      <DrawerBody>
        {error ? (
          <MessageBar intent="error" className="mb-4">
            <MessageBarBody>{error}</MessageBarBody>
          </MessageBar>
        ) : null}

        {isLoadingTypes && leaveTypes.length === 0 ? (
          <Spinner label="Loading leave types..." />
        ) : (
          <form className="flex flex-col gap-4" onSubmit={(event) => void handleSubmit(event)}>
            {canCreateOnBehalf ? (
              <Field
                label="Employee"
                required
                hint="Select yourself or another employee to apply on their behalf."
              >
                <Combobox
                  placeholder="Search employees"
                  value={
                    employeeQuery
                    || selectedEmployee?.displayName
                    || selectedEmployee?.email
                    || (onBehalfOfUserId === user?.id
                      ? (user?.displayName ?? user?.email ?? 'Me')
                      : '')
                  }
                  selectedOptions={onBehalfOfUserId ? [onBehalfOfUserId] : []}
                  onChange={(event) => setEmployeeQuery(event.target.value)}
                  onOptionSelect={(_, data) => {
                    const nextId = data.optionValue ?? null;
                    setOnBehalfOfUserId(nextId);
                    setEmployeeQuery('');
                    setDocumentFile(null);
                    if (fileInputRef.current) {
                      fileInputRef.current.value = '';
                    }
                  }}
                >
                  {user?.id ? (
                    <Option
                      key={user.id}
                      value={user.id}
                      text={user.displayName ?? user.email ?? 'Me'}
                    >
                      {user.displayName ?? user.email ?? 'Me'} (me)
                    </Option>
                  ) : null}
                  {filteredEmployees
                    .filter((item) => item.id !== user?.id)
                    .map((item) => (
                      <Option
                        key={item.id}
                        value={item.id}
                        text={item.displayName ?? item.email ?? item.username}
                      >
                        {item.displayName ?? item.email ?? item.username}
                      </Option>
                    ))}
                </Combobox>
              </Field>
            ) : null}

            {leaveTypes.length === 0 ? (
              <MessageBar intent="warning">
                <MessageBarBody>
                  {isOnBehalf
                    ? 'No leave types are available for the selected employee.'
                    : 'No leave types are configured. Contact HR to set up leave policies.'}
                </MessageBarBody>
              </MessageBar>
            ) : (
              <>
            <Field label="Leave type" required>
              <Dropdown
                placeholder="Select leave type"
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
                    {type.name}
                  </Option>
                ))}
              </Dropdown>
            </Field>

            {selectedType?.deductsBalance && selectedBalance ? (
              <MessageBar intent={selectedBalance.remaining <= 0 ? 'warning' : 'info'}>
                <MessageBarBody>
                  Remaining balance: {selectedBalance.remaining.toFixed(1)} day(s)
                  {' '}(pending: {selectedBalance.pending.toFixed(1)}, used: {selectedBalance.used.toFixed(1)})
                  {selectedBalance.remaining <= 0
                    ? ' — adjust balances or run accrual before requesting this leave type.'
                    : null}
                </MessageBarBody>
              </MessageBar>
            ) : null}

            {selectedType && selectedType.minNoticeDays > 0 ? (
              <MessageBar intent="info">
                <MessageBarBody>
                  This leave type requires at least {selectedType.minNoticeDays} day(s) notice.
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
              </>
            )}

            <div className="flex gap-2 pt-2">
              <Button type="button" appearance="secondary" onClick={onClose} disabled={isSubmitting}>
                Cancel
              </Button>
              <Button type="submit" appearance="primary" disabled={isSubmitting || leaveTypes.length === 0}>
                {isSubmitting ? 'Submitting...' : isOnBehalf ? 'Submit for employee' : 'Submit request'}
              </Button>
            </div>
          </form>
        )}
      </DrawerBody>
    </OverlayDrawer>
  );
}
