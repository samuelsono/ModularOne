import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  Badge,
  Button,
  Checkbox,
  Combobox,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  SpinButton,
  Spinner,
  Subtitle2,
  Text,
  createTableColumn,
} from '@fluentui/react-components';
import { AddRegular, BinRecycleRegular, EditRegular } from '@fluentui/react-icons';
import { ApiError } from '@platform/api/apiClient';
import {
  createLeaveType,
  deleteLeaveType,
  getAdminLeaveTypes,
  updateLeaveType,
  runLeaveAccrual,
} from '@modules/leave/services/leaveService';
import type { LeaveType, SaveLeaveTypeRequest } from '@modules/leave/types/leave';
import AppTitle from '@platform/ui/AppTitle';
import { withAuditableColumns } from '@platform/ui/auditTableColumns';
import { LeaveNoticeDialog } from './LeaveNoticeDialog';
import { usePageSearchQuery } from '@platform/shell/PageSearchContext';
import { filterLeaveTypes } from '@modules/leave/search/filters';

function parseOptionalNumber(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const parsed = typeof value === 'number' ? value : Number.parseFloat(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function parseOptionalInt(value: number | string | null | undefined): number | null {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const parsed = typeof value === 'number' ? value : Number.parseInt(String(value), 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function serializeLeaveTypeForm(form: SaveLeaveTypeRequest): SaveLeaveTypeRequest {
  return {
    ...form,
    annualEntitlement: parseOptionalNumber(form.annualEntitlement ?? null),
    maxConsecutiveDays: parseOptionalInt(form.maxConsecutiveDays ?? null),
    minNoticeDays: parseOptionalInt(form.minNoticeDays) ?? 0,
    sortOrder: parseOptionalInt(form.sortOrder) ?? 0,
  };
}

const EMPTY_FORM: SaveLeaveTypeRequest = {
  name: '',
  code: '',
  color: '#0078D4',
  isPaid: true,
  deductsBalance: true,
  requiresDocument: false,
  allowHalfDay: true,
  accrualMethod: 'Upfront',
  annualEntitlement: null,
  maxConsecutiveDays: null,
  minNoticeDays: 0,
  eligibleGender: 'Any',
  isActive: true,
  sortOrder: 0,
};

function LeaveTypeFormDialog({
  open,
  initial,
  onClose,
  onSaved,
}: {
  open: boolean;
  initial: (SaveLeaveTypeRequest & { id?: string }) | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = initial !== null && Boolean(initial.id);
  const [form, setForm] = useState<SaveLeaveTypeRequest>(initial ?? EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setForm(initial ?? EMPTY_FORM);
    setError(null);
  }, [initial, open]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    const payload = serializeLeaveTypeForm(form);

    try {
      if (isEdit && initial?.id) {
        await updateLeaveType(initial.id, payload);
      } else {
        await createLeaveType(payload);
      }
      onSaved();
      onClose();
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save leave type.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form onSubmit={(event) => void handleSubmit(event)}>
          <DialogBody>
            <DialogTitle>{isEdit ? 'Edit leave type' : 'Add leave type'}</DialogTitle>
            <DialogContent className="flex flex-col gap-3 pt-2">
              {error ? (
                <MessageBar intent="error">
                  <MessageBarBody>{error}</MessageBarBody>
                </MessageBar>
              ) : null}

              <Field label="Name" required>
                <Input
                  value={form.name}
                  maxLength={128}
                  onChange={(_, data) => setForm((current) => ({ ...current, name: data.value }))}
                />
              </Field>

              <div className='grid grid-cols-3 gap-x-3'>

              <Field label="Code" required>
                <Input
                  value={form.code}
                  maxLength={32}
                  onChange={(_, data) => setForm((current) => ({ ...current, code: data.value }))}
                />
              </Field>

              <Field label="Color">
                <Input
                  type={"color" as "text"}
                  style={{ minWidth: 32, width: 70, padding: 0 }}
                  value={form.color ?? '#0078D4'}
                  onChange={(_, data) => setForm((current) => ({ ...current, color: data.value }))}
                />
              </Field>

              <Field label="Sort order">
                <SpinButton
                  type="number"
                  style={{ minWidth: 72 }}
                  value={form.sortOrder}
                  onChange={(_, data) => setForm((current) => ({
                    ...current,
                    sortOrder: Number.parseInt(String(data.value ?? ''), 10) || 0,
                  }))}
                />
              </Field>
              </div>

<div className='grid grid-cols-2 gap-3 items-start'>

              <Field
                label="Minimum notice (days)"
                hint="Set to 0 to allow this leave type to be submitted for past dates."
              >
                <SpinButton
                  min={0}
                  value={form.minNoticeDays}
                  onChange={(_, data) => setForm((current) => ({
                    ...current,
                    minNoticeDays: data.value || 0,
                  }))}
                />
              </Field>

              <Field label="Max consecutive days">
                <SpinButton
                  type="number"
                  value={form.maxConsecutiveDays ?? undefined}
                  placeholder="No limit"
                  onChange={(_, data) => setForm((current) => ({
                    ...current,
                    maxConsecutiveDays: data.value ?? null,
                  }))}
                />
              </Field>
</div>

              <Field
                label="Gender eligibility"
                hint="Restricted leave types are hidden from ineligible employees and cannot be requested."
              >
                <Combobox
                  value={form.eligibleGender}
                  selectedOptions={[form.eligibleGender]}
                  onOptionSelect={(_, data) => setForm((current) => ({
                    ...current,
                    eligibleGender: (data.optionValue ?? 'Any') as SaveLeaveTypeRequest['eligibleGender'],
                  }))}
                >
                  <Option value="Any">Any gender</Option>
                  <Option value="Male">Male only</Option>
                  <Option value="Female">Female only</Option>
                </Combobox>
              </Field>


              <div className='grid grid-cols-2 gap-x-3'>


              <Checkbox
                label="Paid leave"
                checked={form.isPaid}
                onChange={(_, data) => setForm((current) => ({
                  ...current,
                  isPaid: Boolean(data.checked),
                }))}
              />

              <Checkbox
                label="Deducts from balance"
                checked={form.deductsBalance}
                onChange={(_, data) => setForm((current) => ({
                  ...current,
                  deductsBalance: Boolean(data.checked),
                }))}
              />

              <Checkbox
                label="Requires supporting document"
                checked={form.requiresDocument}
                onChange={(_, data) => setForm((current) => ({
                  ...current,
                  requiresDocument: Boolean(data.checked),
                }))}
              />

              <Checkbox
                label="Allow half-day requests"
                checked={form.allowHalfDay}
                onChange={(_, data) => setForm((current) => ({
                  ...current,
                  allowHalfDay: Boolean(data.checked),
                }))}
              />
              </div>


              <Field label="Accrual method">
                <Combobox
                  value={form.accrualMethod === 'Monthly' ? 'Monthly accrual' : 'Upfront (year-start allocation)'}
                  selectedOptions={[form.accrualMethod]}
                  onOptionSelect={(_, data) => setForm((current) => ({
                    ...current,
                    accrualMethod: data.optionValue ?? 'Upfront',
                  }))}
                >
                  <Option value="Upfront" text="Upfront (year-start allocation)">
                    Upfront (year-start allocation)
                  </Option>
                  <Option value="Monthly" text="Monthly accrual">
                    Monthly accrual
                  </Option>
                </Combobox>
              </Field>


              <Field label="Annual entitlement (days)" hint="Used for upfront or monthly accrual">
                <Input
                  type="number"
                  step={0.5}
                  min={0}
                  value={form.annualEntitlement != null ? String(form.annualEntitlement) : ''}
                  placeholder="No entitlement"
                  onChange={(_, data) => setForm((current) => ({
                    ...current,
                    annualEntitlement: data.value === '' ? null : parseOptionalNumber(data.value),
                  }))}
                />
              </Field>

              <Checkbox
                label="Active"
                checked={form.isActive}
                onChange={(_, data) => setForm((current) => ({
                  ...current,
                  isActive: Boolean(data.checked),
                }))}
              />
            </DialogContent>
          </DialogBody>
          <DialogActions>
            <Button type="button" appearance="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" appearance="primary" disabled={isSaving}>
              {isSaving ? 'Saving...' : 'Save'}
            </Button>
          </DialogActions>
        </form>
      </DialogSurface>
    </Dialog>
  );
}

interface LeaveTypesManagerProps {
  canWrite: boolean;
}

export function LeaveTypesManager({ canWrite }: LeaveTypesManagerProps) {
  const searchQuery = usePageSearchQuery();
  const [types, setTypes] = useState<LeaveType[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingType, setEditingType] = useState<(SaveLeaveTypeRequest & { id?: string }) | null>(null);
  const [isRunningAccrual, setIsRunningAccrual] = useState(false);
  const [accrualNotice, setAccrualNotice] = useState<string | null>(null);


  const columnSizingOptions = useMemo(() => ({
    name: { minWidth: 150, maxWidth: 300 },
    accrual: { minWidth: 150, maxWidth: 250 },
    code: { minWidth: 100, maxWidth: 150 },
    flags: { minWidth: 100, maxWidth: 500, idealWidth: 200 },
    sortOrder: { minWidth: 50, maxWidth: 100 },
    isActive: { minWidth: 50, maxWidth: 100 },
    actions: { minWidth: 100, maxWidth: 150 },
  }), []);

  const loadTypes = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getAdminLeaveTypes();
      setTypes(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load leave types.');
      setTypes([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadTypes();
  }, [loadTypes]);

  const filteredTypes = useMemo(
    () => filterLeaveTypes(types, searchQuery),
    [searchQuery, types],
  );

  const columns: TableColumnDefinition<LeaveType>[] = useMemo(
    () => withAuditableColumns([
      createTableColumn<LeaveType>({
        columnId: 'name',
        renderHeaderCell: () => 'Name',
        renderCell: (item) => (
          <span className="inline-flex items-center gap-2">
            <span
              className="inline-block w-3 h-3 rounded-full shrink-0"
              style={{ backgroundColor: item.color }}
            />
            {item.name}
          </span>
        ),
      }),
      createTableColumn<LeaveType>({
        columnId: 'accrual',
        renderHeaderCell: () => 'Accrual',
        renderCell: (item) => (
          item.accrualMethod === 'Monthly'
            ? `Monthly (${item.annualEntitlement ?? '—'} d/yr)`
            : `Upfront (${item.annualEntitlement ?? '—'} d)`
        ),
      }),
      createTableColumn<LeaveType>({
        columnId: 'code',
        renderHeaderCell: () => 'Code',
        renderCell: (item) => item.code,
      }),
      createTableColumn<LeaveType>({
        columnId: 'flags',
        renderHeaderCell: () => 'Rules',
        renderCell: (item) => (
          <div className="flex flex-wrap gap-1">
            {item.isPaid ? <Badge appearance="filled" size="small">Paid</Badge> : null}
            {item.deductsBalance ? <Badge color='important' appearance="filled" size="small">Will Deduct</Badge> : null}
            {item.requiresDocument ? <Badge color='important' appearance="filled" size="small">Document</Badge> : null}
            {item.eligibleGender !== 'Any' ? (
              <Badge color="warning" appearance="filled" size="small">
                {item.eligibleGender} only
              </Badge>
            ) : null}
            {item.minNoticeDays > 0 ? (
              <Badge color='danger' appearance="filled" size="small">{item.minNoticeDays}d notice</Badge>
            ) : (
              <Badge color="informative" appearance="filled" size="small">Past dates allowed</Badge>
            )}
          </div>
        ),
      }),
      createTableColumn<LeaveType>({
        columnId: 'sortOrder',
        renderHeaderCell: () => 'Sort',
        renderCell: (item) => item.sortOrder,
      }),
      createTableColumn<LeaveType>({
        columnId: 'isActive',
        renderHeaderCell: () => 'Active',
        renderCell: (item) => (item.isActive ? 'Yes' : 'No'),
      }),
      ...(canWrite
        ? [
            createTableColumn<LeaveType>({
              columnId: 'actions',
              renderHeaderCell: () => 'Actions',
              renderCell: (item) => (
                <div className="flex gap-2">
                  <Button
                    appearance="subtle"
                    icon={<EditRegular />}
                    onClick={() => {
                      setEditingType({
                        id: item.id,
                        name: item.name,
                        code: item.code,
                        color: item.color,
                        isPaid: item.isPaid,
                        deductsBalance: item.deductsBalance,
                        requiresDocument: item.requiresDocument,
                        allowHalfDay: item.allowHalfDay,
                        accrualMethod: item.accrualMethod,
                        annualEntitlement: item.annualEntitlement,
                        maxConsecutiveDays: item.maxConsecutiveDays,
                        minNoticeDays: item.minNoticeDays,
                        eligibleGender: item.eligibleGender,
                        isActive: item.isActive,
                        sortOrder: item.sortOrder,
                      });
                      setDialogOpen(true);
                    }}
                  >
                  </Button>
                  <Button
                    appearance={"transparent"}
                    color='danger'
                    icon={<BinRecycleRegular color='danger' />}
                    onClick={() => {
                      void deleteLeaveType(item.id)
                        .then(() => loadTypes())
                        .catch((deleteError) => {
                          setError(deleteError instanceof ApiError
                            ? deleteError.message
                            : 'Failed to delete leave type.');
                        });
                    }}
                  >
                  </Button>
                </div>
              ),
            }),
          ]
        : []),
    ]),
    [canWrite, loadTypes],
  );

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-start justify-between gap-4 px-3">
        <AppTitle title="Leave types" subtitle="Configure the leave types employees can request." />
        {canWrite ? (
          <div className="flex gap-2">
            <Button
              appearance="secondary"
              disabled={isRunningAccrual}
              onClick={() => {
                setIsRunningAccrual(true);
                void runLeaveAccrual()
                  .then((result) => {
                    setError(null);
                    setAccrualNotice(
                      result.applied === 1
                        ? 'Applied 1 accrual entry.'
                        : `Applied ${result.applied} accrual entries.`,
                    );
                  })
                  .catch((accrualError) => {
                    setError(accrualError instanceof ApiError
                      ? accrualError.message
                      : 'Failed to run leave accrual.');
                  })
                  .finally(() => setIsRunningAccrual(false));
              }}
            >
              {isRunningAccrual ? 'Running accrual...' : 'Run accrual'}
            </Button>
            <Button
              appearance="primary"
              icon={<AddRegular />}
              onClick={() => {
                setEditingType(null);
                setDialogOpen(true);
              }}
            >
              Add type
            </Button>
          </div>
        ) : null}
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      {isLoading ? (
        <Spinner label="Loading leave types..." />
      ) : types.length === 0 ? (
        <Text className="text-sm text-neutral-foreground-3">
          No leave types configured yet.
        </Text>
      ) : filteredTypes.length === 0 ? (
        <Text className="text-sm text-neutral-foreground-3">
          No leave types match your search.
        </Text>
      ) : (
        <DataGrid items={filteredTypes} columnSizingOptions={columnSizingOptions} resizableColumns columns={columns} getRowId={(item) => item.id}>
          <DataGridHeader>
            <DataGridRow>
              {({ renderHeaderCell }) => (
                <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
              )}
            </DataGridRow>
          </DataGridHeader>
          <DataGridBody<LeaveType>>
            {({ item, rowId }) => (
              <DataGridRow<LeaveType> key={rowId}>
                {({ renderCell }) => (
                  <DataGridCell>{renderCell(item)}</DataGridCell>
                )}
              </DataGridRow>
            )}
          </DataGridBody>
        </DataGrid>
      )}

      {canWrite ? (
        <LeaveTypeFormDialog
          open={dialogOpen}
          initial={editingType}
          onClose={() => setDialogOpen(false)}
          onSaved={() => void loadTypes()}
        />
      ) : null}

      <LeaveNoticeDialog
        open={accrualNotice !== null}
        title="Accrual complete"
        message={accrualNotice ?? ''}
        onClose={() => setAccrualNotice(null)}
      />
    </div>
  );
}
