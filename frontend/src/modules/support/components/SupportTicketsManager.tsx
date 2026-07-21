import { useCallback, useEffect, useMemo, useState, type MouseEvent } from 'react';
import type { TableColumnDefinition } from '@fluentui/react-components';
import {
  Badge,
  Button,
  Combobox,
  DataGrid,
  DataGridBody,
  DataGridCell,
  DataGridHeader,
  DataGridHeaderCell,
  DataGridRow,
  Dropdown,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Option,
  Spinner,
  Subtitle2,
  Text,
  Textarea,
  createTableColumn,
} from '@fluentui/react-components';
import { ApiError } from '@platform/api/apiClient';
import { getAuthUsersForBroadcast, type DirectoryAuthUserLookup } from '@platform/org/directoryApi';
import {
  deleteTicket,
  getAllTickets,
  updateTicket,
} from '@modules/support/services/supportService';
import type {
  SupportTicket,
  TicketPriority,
  TicketStatus,
  TicketType,
  UpdateTicketAdminRequest,
} from '@modules/support/types/support';
import { stopDataGridRowSelection } from '@platform/utils/dataGrid';

const TICKET_TYPES: TicketType[] = ['Ticket', 'Feedback', 'BugReport'];
const TICKET_STATUSES: TicketStatus[] = ['Open', 'InProgress', 'Resolved', 'Closed'];
const TICKET_PRIORITIES: TicketPriority[] = ['Low', 'Medium', 'High', 'Critical'];

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString('en-ZA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function typeBadgeColor(type: TicketType): 'informative' | 'success' | 'danger' {
  if (type === 'BugReport') {
    return 'danger';
  }
  if (type === 'Feedback') {
    return 'success';
  }
  return 'informative';
}

interface TicketDetailPanelProps {
  ticket: SupportTicket;
  users: DirectoryAuthUserLookup[];
  onClose: () => void;
  onSaved: (ticket: SupportTicket) => void;
  onDeleted: (id: string) => void;
}

function TicketDetailPanel({
  ticket,
  users,
  onClose,
  onSaved,
  onDeleted,
}: TicketDetailPanelProps) {
  const [status, setStatus] = useState<TicketStatus>(ticket.status);
  const [priority, setPriority] = useState<TicketPriority>(ticket.priority);
  const [assignedToUserId, setAssignedToUserId] = useState(ticket.assignedToDisplayName ?? '');
  const [adminNotes, setAdminNotes] = useState(ticket.adminNotes ?? '');
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setStatus(ticket.status);
    setPriority(ticket.priority);
    setAssignedToUserId(
      users.find((user) => user.displayName === ticket.assignedToDisplayName)?.id ?? '',
    );
    setAdminNotes(ticket.adminNotes ?? '');
  }, [ticket, users]);

  const selectedAssigneeLabel = users.find((user) => user.id === assignedToUserId)?.displayName ?? '';

  async function handleSave() {
    setIsSaving(true);
    setError(null);

    const payload: UpdateTicketAdminRequest = {
      status,
      priority,
      assignedToUserId: assignedToUserId || null,
      adminNotes: adminNotes.trim() || null,
    };

    try {
      const updated = await updateTicket(ticket.id, payload);
      onSaved(updated);
    } catch (saveError) {
      setError(saveError instanceof ApiError ? saveError.message : 'Failed to save changes.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete() {
    setIsDeleting(true);
    setError(null);

    try {
      await deleteTicket(ticket.id);
      onDeleted(ticket.id);
    } catch (deleteError) {
      setError(deleteError instanceof ApiError ? deleteError.message : 'Failed to delete ticket.');
    } finally {
      setIsDeleting(false);
    }
  }

  return (
    <aside className="w-[360px] shrink-0 border-l border-neutral-stroke-2 p-4 flex flex-col gap-4 overflow-y-auto">
      <div className="flex items-start justify-between gap-2">
        <div>
          <Subtitle2>{ticket.subject}</Subtitle2>
          <Text className="text-xs text-neutral-foreground-3">
            {ticket.submittedByDisplayName} · {formatDateTime(ticket.createdAt)}
          </Text>
        </div>
        <Button appearance="subtle" onClick={onClose}>Close</Button>
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Badge appearance="outline" color={typeBadgeColor(ticket.type)}>{ticket.type}</Badge>
        <Badge appearance="outline">{ticket.category.label}</Badge>
        <Badge appearance="outline">{ticket.status}</Badge>
      </div>

      <Text className="text-sm whitespace-pre-wrap">{ticket.description}</Text>

      {ticket.type === 'BugReport' ? (
        <div className="text-sm flex flex-col gap-2">
          {ticket.bugSeverity ? <Text><strong>Severity:</strong> {ticket.bugSeverity}</Text> : null}
          {ticket.stepsToReproduce ? (
            <Text><strong>Steps:</strong> {ticket.stepsToReproduce}</Text>
          ) : null}
          {ticket.expectedBehavior ? (
            <Text><strong>Expected:</strong> {ticket.expectedBehavior}</Text>
          ) : null}
          {ticket.actualBehavior ? (
            <Text><strong>Actual:</strong> {ticket.actualBehavior}</Text>
          ) : null}
          {ticket.browserOrEnvironment ? (
            <Text><strong>Environment:</strong> {ticket.browserOrEnvironment}</Text>
          ) : null}
        </div>
      ) : null}

      {ticket.satisfactionRating ? (
        <Text className="text-sm">Rating: {'★'.repeat(ticket.satisfactionRating)}</Text>
      ) : null}

      <Field label="Status">
        <Dropdown
          value={status}
          selectedOptions={[status]}
          onOptionSelect={(_, data) => {
            const next = data.optionValue as TicketStatus | undefined;
            if (next) {
              setStatus(next);
            }
          }}
        >
          {TICKET_STATUSES.map((item) => (
            <Option key={item} value={item}>{item}</Option>
          ))}
        </Dropdown>
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
          {TICKET_PRIORITIES.map((item) => (
            <Option key={item} value={item}>{item}</Option>
          ))}
        </Dropdown>
      </Field>

      <Field label="Assigned to">
        <Combobox
          value={selectedAssigneeLabel}
          selectedOptions={assignedToUserId ? [assignedToUserId] : []}
          onOptionSelect={(_, data) => setAssignedToUserId(data.optionValue ?? '')}
        >
          <Option value="" text="Unassigned">Unassigned</Option>
          {users.map((user) => {
            const label = user.displayName || user.email || user.username;
            return (
              <Option key={user.id} value={user.id} text={label}>{label}</Option>
            );
          })}
        </Combobox>
      </Field>

      <Field label="Admin notes">
        <Textarea
          value={adminNotes}
          maxLength={4000}
          resize="vertical"
          rows={4}
          onChange={(_, data) => setAdminNotes(data.value)}
        />
      </Field>

      <div className="flex gap-2">
        <Button appearance="primary" onClick={() => void handleSave()} disabled={isSaving}>
          {isSaving ? 'Saving...' : 'Save changes'}
        </Button>
        <Button appearance="secondary" onClick={() => void handleDelete()} disabled={isDeleting}>
          {isDeleting ? 'Deleting...' : 'Delete'}
        </Button>
      </div>
    </aside>
  );
}

export function SupportTicketsManager() {
  const [tickets, setTickets] = useState<SupportTicket[]>([]);
  const [users, setUsers] = useState<DirectoryAuthUserLookup[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState<TicketType | ''>('');
  const [statusFilter, setStatusFilter] = useState<TicketStatus | ''>('');
  const [priorityFilter, setPriorityFilter] = useState<TicketPriority | ''>('');
  const [search, setSearch] = useState('');
  const [selectedTicketId, setSelectedTicketId] = useState<string | null>(null);

  const loadTickets = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const items = await getAllTickets({
        type: typeFilter || undefined,
        status: statusFilter || undefined,
        priority: priorityFilter || undefined,
        search: search.trim() || undefined,
      });
      setTickets(items);
    } catch (loadError) {
      setError(loadError instanceof ApiError ? loadError.message : 'Failed to load tickets.');
      setTickets([]);
    } finally {
      setIsLoading(false);
    }
  }, [typeFilter, statusFilter, priorityFilter, search]);

  useEffect(() => {
    void loadTickets();
  }, [loadTickets]);

  useEffect(() => {
    void getAuthUsersForBroadcast()
      .then(setUsers)
      .catch(() => setUsers([]));
  }, []);

  const selectedTicket = tickets.find((ticket) => ticket.id === selectedTicketId) ?? null;

  const columns: TableColumnDefinition<SupportTicket>[] = useMemo(
    () => [
      createTableColumn<SupportTicket>({
        columnId: 'type',
        renderHeaderCell: () => 'Type',
        renderCell: (item) => (
          <Badge appearance="outline" color={typeBadgeColor(item.type)}>{item.type}</Badge>
        ),
      }),
      createTableColumn<SupportTicket>({
        columnId: 'subject',
        renderHeaderCell: () => 'Subject',
        renderCell: (item) => item.subject,
      }),
      createTableColumn<SupportTicket>({
        columnId: 'category',
        renderHeaderCell: () => 'Category',
        renderCell: (item) => item.category.label,
      }),
      createTableColumn<SupportTicket>({
        columnId: 'status',
        renderHeaderCell: () => 'Status',
        renderCell: (item) => item.status,
      }),
      createTableColumn<SupportTicket>({
        columnId: 'priority',
        renderHeaderCell: () => 'Priority',
        renderCell: (item) => item.priority,
      }),
      createTableColumn<SupportTicket>({
        columnId: 'submittedBy',
        renderHeaderCell: () => 'Submitted by',
        renderCell: (item) => item.submittedByDisplayName,
      }),
      createTableColumn<SupportTicket>({
        columnId: 'createdAt',
        renderHeaderCell: () => 'Created',
        renderCell: (item) => formatDateTime(item.createdAt),
      }),
    ],
    [],
  );

  return (
    <div className="flex flex-col gap-4 h-full min-h-0">
      <div>
        <Subtitle2>Support tickets</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3">
          Review and manage support tickets, feedback, and bug reports submitted by users.
        </Text>
      </div>

      <div className="flex flex-wrap gap-3 items-end">
        <Field label="Type">
          <Dropdown
            value={typeFilter || 'All'}
            selectedOptions={typeFilter ? [typeFilter] : ['']}
            onOptionSelect={(_, data) => setTypeFilter((data.optionValue ?? '') as TicketType | '')}
          >
            <Option value="">All</Option>
            {TICKET_TYPES.map((item) => (
              <Option key={item} value={item}>{item}</Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Status">
          <Dropdown
            value={statusFilter || 'All'}
            selectedOptions={statusFilter ? [statusFilter] : ['']}
            onOptionSelect={(_, data) => setStatusFilter((data.optionValue ?? '') as TicketStatus | '')}
          >
            <Option value="">All</Option>
            {TICKET_STATUSES.map((item) => (
              <Option key={item} value={item}>{item}</Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Priority">
          <Dropdown
            value={priorityFilter || 'All'}
            selectedOptions={priorityFilter ? [priorityFilter] : ['']}
            onOptionSelect={(_, data) => setPriorityFilter((data.optionValue ?? '') as TicketPriority | '')}
          >
            <Option value="">All</Option>
            {TICKET_PRIORITIES.map((item) => (
              <Option key={item} value={item}>{item}</Option>
            ))}
          </Dropdown>
        </Field>

        <Field label="Search" className="min-w-[220px]">
          <Input
            value={search}
            placeholder="Subject, description, submitter..."
            onChange={(_, data) => setSearch(data.value)}
          />
        </Field>

        <Button appearance="secondary" onClick={() => void loadTickets()}>Refresh</Button>
      </div>

      {error ? (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      ) : null}

      <div className="flex flex-1 min-h-0 gap-0">
        <div className="flex-1 min-w-0 overflow-auto">
          {isLoading ? (
            <Spinner label="Loading tickets..." />
          ) : (
            <DataGrid items={tickets} columns={columns} getRowId={(item) => item.id}>
              <DataGridHeader>
                <DataGridRow>
                  {({ renderHeaderCell }) => (
                    <DataGridHeaderCell>{renderHeaderCell()}</DataGridHeaderCell>
                  )}
                </DataGridRow>
              </DataGridHeader>
              <DataGridBody<SupportTicket>>
                {({ item, rowId }) => (
                  <DataGridRow<SupportTicket>
                    key={rowId}
                    onClick={(event: MouseEvent<HTMLTableRowElement>) => {
                      stopDataGridRowSelection(event);
                      setSelectedTicketId(item.id);
                    }}
                    className={selectedTicketId === item.id ? 'bg-neutral-background-2' : undefined}
                  >
                    {({ renderCell }) => (
                      <DataGridCell>{renderCell(item)}</DataGridCell>
                    )}
                  </DataGridRow>
                )}
              </DataGridBody>
            </DataGrid>
          )}
        </div>

        {selectedTicket ? (
          <TicketDetailPanel
            ticket={selectedTicket}
            users={users}
            onClose={() => setSelectedTicketId(null)}
            onSaved={(updated) => {
              setTickets((current) => current.map((item) => (
                item.id === updated.id ? updated : item
              )));
            }}
            onDeleted={(id) => {
              setTickets((current) => current.filter((item) => item.id !== id));
              setSelectedTicketId(null);
            }}
          />
        ) : null}
      </div>
    </div>
  );
}
