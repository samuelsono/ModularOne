import React from 'react';
import { Badge, Button, Card, Spinner, Text, tokens } from '@fluentui/react-components';
import { History24Regular } from '@fluentui/react-icons';
import type { LeaveHistoryItem, LeaveRequestStatus } from '../types/leave';

const muted = { color: tokens.colorNeutralForeground3 };

type LeaveHistoryListProps = {
  items: LeaveHistoryItem[];
  loading?: boolean;
  error?: string | null;
  onRetry?: () => void;
};

function formatDate(value: string): string {
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) {
    return value || '—';
  }
  return new Intl.DateTimeFormat(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(parsed));
}

function formatDateRange(startDate: string, endDate: string): string {
  const start = formatDate(startDate);
  const end = formatDate(endDate);
  if (start === end) {
    return start;
  }
  return `${start} – ${end}`;
}

function badgeColor(status: LeaveRequestStatus): 'warning' | 'success' | 'danger' | 'informative' {
  switch (String(status).toLowerCase()) {
    case 'approved':
      return 'success';
    case 'rejected':
      return 'danger';
    case 'cancelled':
      return 'informative';
    default:
      return 'warning';
  }
}

const LeaveHistoryList: React.FC<LeaveHistoryListProps> = ({
  items,
  loading = false,
  error = null,
  onRetry,
}) => {
  return (
    <section className="leave-history my-3">
      <div className="leave-history-header mb-3 px-3">
        <History24Regular />
        <Text as="h2" weight="medium" size={400}>
          Leave history
        </Text>
      </div>

      {loading && (
        <Card className="app-card leave-history-state">
          <Spinner size="small" label="Loading leave history…" />
        </Card>
      )}

      {!loading && error && (
        <Card className="app-card leave-history-state">
          <Text block style={{ color: tokens.colorPaletteRedForeground1 }}>
            {error}
          </Text>
          {onRetry && (
            <Button appearance="secondary" onClick={onRetry} style={{ marginTop: 12 }}>
              Retry
            </Button>
          )}
        </Card>
      )}

      {!loading && !error && items.length === 0 && (
        <Card className="app-card leave-history-state">
          <Text style={muted}>No leave history found.</Text>
        </Card>
      )}

      {!loading &&
        !error &&
        items.map((item) => (
          <Card key={item.id} className="app-card shadow-xs!">
            <div className="leave-history-row">
              <div className="leave-history-copy">
                <Text weight="semibold" block>
                  {item.type}
                </Text>
                
                <Text size={300} style={muted} block>
                  {item.workingDays.toFixed(1)} working day{item.workingDays === 1 ? '' : 's'}
                </Text>
              </div>
              <div className="flex flex-col items-end gap-1">
              <Badge appearance="filled" color={badgeColor(item.status)}>
                {item.status}
              </Badge>
              <Text size={300} style={muted} block>
                  {formatDateRange(item.startDate, item.endDate)}
                </Text>
              </div>

            </div>
          </Card>
        ))}
    </section>
  );
};

export default LeaveHistoryList;
