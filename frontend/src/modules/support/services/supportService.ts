import type {
  SaveCategoryRequest,
  SubmitTicketRequest,
  SupportTicket,
  TicketCategory,
  TicketPriority,
  TicketStatus,
  TicketType,
  UpdateTicketAdminRequest,
} from '@modules/support/types/support';
import { authorizedFetch } from '@platform/api/authService';

export async function getCategories(type?: TicketType): Promise<TicketCategory[]> {
  const query = type ? `?type=${encodeURIComponent(type)}` : '';
  return authorizedFetch<TicketCategory[]>(`/api/support/categories${query}`);
}

export async function submitTicket(data: SubmitTicketRequest): Promise<SupportTicket> {
  return authorizedFetch<SupportTicket>('/api/support/tickets', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function getMyTickets(): Promise<SupportTicket[]> {
  return authorizedFetch<SupportTicket[]>('/api/support/tickets');
}

export async function getTicket(id: string): Promise<SupportTicket> {
  return authorizedFetch<SupportTicket>(`/api/support/tickets/${encodeURIComponent(id)}`);
}

export async function getAllTickets(filters?: {
  type?: TicketType;
  status?: TicketStatus;
  priority?: TicketPriority;
  search?: string;
}): Promise<SupportTicket[]> {
  const params = new URLSearchParams();
  if (filters?.type) {
    params.set('type', filters.type);
  }
  if (filters?.status) {
    params.set('status', filters.status);
  }
  if (filters?.priority) {
    params.set('priority', filters.priority);
  }
  if (filters?.search?.trim()) {
    params.set('search', filters.search.trim());
  }

  const query = params.toString();
  return authorizedFetch<SupportTicket[]>(`/api/support/admin/tickets${query ? `?${query}` : ''}`);
}

export async function updateTicket(
  id: string,
  data: UpdateTicketAdminRequest,
): Promise<SupportTicket> {
  return authorizedFetch<SupportTicket>(`/api/support/admin/tickets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteTicket(id: string): Promise<void> {
  await authorizedFetch<void>(`/api/support/admin/tickets/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export async function getAdminCategories(): Promise<TicketCategory[]> {
  return authorizedFetch<TicketCategory[]>('/api/support/admin/categories');
}

export async function createCategory(data: SaveCategoryRequest): Promise<TicketCategory> {
  return authorizedFetch<TicketCategory>('/api/support/admin/categories', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function updateCategory(
  id: string,
  data: SaveCategoryRequest,
): Promise<TicketCategory> {
  return authorizedFetch<TicketCategory>(`/api/support/admin/categories/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function deleteCategory(id: string): Promise<void> {
  await authorizedFetch<void>(`/api/support/admin/categories/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}
