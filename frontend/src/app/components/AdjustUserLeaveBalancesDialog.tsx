import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button,
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
  Select,
  SpinButton,
  Spinner,
  Text,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import {
  adjustLeaveBalance,
  getUserLeaveBalances,
} from '@modules/leave/services/leaveService';
import type { LeaveBalance } from '@modules/leave/types/leave';
import type { UserListItem } from '@modules/users/types/user';

interface AdjustUserLeaveBalancesDialogProps {
  open: boolean;
  user: UserListItem | null;
  onClose: () => void;
}

export function AdjustUserLeaveBalancesDialog({
  open,
  user,
  onClose,
}: AdjustUserLeaveBalancesDialogProps) {
  const currentYear = new Date().getFullYear();
  const [year, setYear] = useState(currentYear);
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [selectedBalanceId, setSelectedBalanceId] = useState('');
  const [allocatedDelta, setAllocatedDelta] = useState('0');
  const [adjustedDelta, setAdjustedDelta] = useState('0');
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const selectedBalance = useMemo(
    () => balances.find((balance) => balance.id === selectedBalanceId) ?? null,
    [balances, selectedBalanceId],
  );

  const loadBalances = useCallback(async () => {
    if (!open || !user) {
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const items = await getUserLeaveBalances(user.id, year);
      setBalances(items);
      setSelectedBalanceId((current) =>
        items.some((balance) => balance.id === current)
          ? current
          : (items[0]?.id ?? ''),
      );
    } catch (loadError) {
      setBalances([]);
      setSelectedBalanceId('');
      setError(
        loadError instanceof ApiError
          ? loadError.message
          : 'Failed to load this user’s leave balances.',
      );
    } finally {
      setIsLoading(false);
    }
  }, [open, user, year]);

  useEffect(() => {
    if (!open) {
      return;
    }

    setYear(currentYear);
    setAllocatedDelta('0');
    setAdjustedDelta('0');
    setSuccess(null);
    setError(null);
  }, [currentYear, open, user?.id]);

  useEffect(() => {
    void loadBalances();
  }, [loadBalances]);

  useEffect(() => {
    setAllocatedDelta('0');
    setAdjustedDelta('0');
    setSuccess(null);
  }, [selectedBalanceId]);

  async function saveAdjustment() {
    if (!selectedBalance || !user) {
      return;
    }

    const allocation = Number.parseFloat(allocatedDelta);
    const adjustment = Number.parseFloat(adjustedDelta);
    if (!Number.isFinite(allocation) || !Number.isFinite(adjustment)) {
      setError('Enter valid numeric values.');
      return;
    }

    if (allocation === 0 && adjustment === 0) {
      setError('Enter an allocation or adjustment change.');
      return;
    }

    setIsSaving(true);
    setError(null);
    setSuccess(null);
    try {
      await adjustLeaveBalance({
        userId: user.id,
        leaveTypeId: selectedBalance.leaveTypeId,
        cycleStart: selectedBalance.cycleStart,
        cycleEnd: selectedBalance.cycleEnd,
        allocatedDelta: allocation,
        adjustedDelta: adjustment,
      });
      setSuccess(`${selectedBalance.leaveTypeName} balance updated.`);
      setAllocatedDelta('0');
      setAdjustedDelta('0');
      await loadBalances();
    } catch (saveError) {
      setError(
        saveError instanceof ApiError
          ? saveError.message
          : 'Failed to adjust the leave balance.',
      );
    } finally {
      setIsSaving(false);
    }
  }

  const allocatedValue = Number.parseFloat(allocatedDelta);
  const adjustedValue = Number.parseFloat(adjustedDelta);

  return (
    <Dialog open={open} modalType="modal" onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface aria-describedby={undefined}>
        <DialogBody>
          <DialogTitle>
            Adjust leave — {user?.displayName ?? user?.username ?? 'User'}
          </DialogTitle>
          <DialogContent className="flex flex-col gap-4 pt-2">
            {error ? (
              <MessageBar intent="error">
                <MessageBarBody>{error}</MessageBarBody>
              </MessageBar>
            ) : null}
            {success ? (
              <MessageBar intent="success">
                <MessageBarBody>{success}</MessageBarBody>
              </MessageBar>
            ) : null}

            <Field label="Balance year">
              <Select
                value={String(year)}
                disabled={isLoading || isSaving}
                onChange={(event) => setYear(Number(event.target.value))}
              >
                {[currentYear - 1, currentYear, currentYear + 1].map((value) => (
                  <option key={value} value={value}>{value}</option>
                ))}
              </Select>
            </Field>

            {isLoading ? (
              <Spinner label="Loading leave balances…" />
            ) : balances.length === 0 ? (
              <Text>No adjustable leave balances are available for this user and year.</Text>
            ) : (
              <>
                <Field label="Leave type" required>
                  <Select
                    value={selectedBalanceId}
                    disabled={isSaving}
                    onChange={(event) => setSelectedBalanceId(event.target.value)}
                  >
                    {balances.map((balance) => (
                      <option key={balance.id} value={balance.id}>
                        {balance.leaveTypeName}
                      </option>
                    ))}
                  </Select>
                </Field>

                {selectedBalance ? (
                  <div className="grid grid-cols-2 gap-3 rounded border border-neutral-stroke-3 p-3">
                    <Text>Allocated: {selectedBalance.allocated.toFixed(1)}</Text>
                    <Text>Adjusted: {selectedBalance.adjusted.toFixed(1)}</Text>
                    <Text>Used: {selectedBalance.used.toFixed(1)}</Text>
                    <Text>Pending: {selectedBalance.pending.toFixed(1)}</Text>
                    <Text weight="semibold">
                      Remaining: {selectedBalance.remaining.toFixed(1)}
                    </Text>
                  </div>
                ) : null}

                <Field
                  label="Allocation change"
                  hint="Use a negative value to reduce the allocation."
                >
                  <SpinButton
                    step={0.5}
                    value={Number.isFinite(allocatedValue) ? allocatedValue : 0}
                    disabled={isSaving}
                    onChange={(_, data) => {
                      if (data.value != null && Number.isFinite(data.value)) {
                        setAllocatedDelta(String(data.value));
                        return;
                      }
                      if (data.displayValue != null && data.displayValue.trim() !== '') {
                        setAllocatedDelta(data.displayValue);
                      }
                    }}
                  />
                </Field>
                <Field
                  label="Manual adjustment"
                  hint="Use a positive or negative value."
                >
                  <SpinButton
                    step={0.5}
                    value={Number.isFinite(adjustedValue) ? adjustedValue : 0}
                    disabled={isSaving}
                    onChange={(_, data) => {
                      if (data.value != null && Number.isFinite(data.value)) {
                        setAdjustedDelta(String(data.value));
                        return;
                      }
                      if (data.displayValue != null && data.displayValue.trim() !== '') {
                        setAdjustedDelta(data.displayValue);
                      }
                    }}
                  />
                </Field>
              </>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose} disabled={isSaving}>
              Close
            </Button>
            <Button
              appearance="primary"
              onClick={() => void saveAdjustment()}
              disabled={isLoading || isSaving || !selectedBalance}
            >
              {isSaving ? 'Saving…' : 'Apply adjustment'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
