export type TicketType = 'Ticket' | 'Feedback' | 'BugReport';
export type TicketStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type BugSeverity = 'Minor' | 'Major' | 'Critical' | 'Blocker';

export interface TicketCategory {
  id: string;
  label: string;
  appliesTo: TicketType[];
  isActive: boolean;
  sortOrder: number;
}

export interface SubmitTicketRequest {
  type: TicketType;
  subject: string;
  description: string;
  categoryId: string;
  priority: TicketPriority;
  bugSeverity?: BugSeverity;
  stepsToReproduce?: string;
  expectedBehavior?: string;
  actualBehavior?: string;
  browserOrEnvironment?: string;
  satisfactionRating?: number;
}

export interface SupportTicket {
  id: string;
  type: TicketType;
  status: TicketStatus;
  priority: TicketPriority;
  subject: string;
  description: string;
  category: TicketCategory;
  bugSeverity?: BugSeverity;
  stepsToReproduce?: string;
  expectedBehavior?: string;
  actualBehavior?: string;
  browserOrEnvironment?: string;
  satisfactionRating?: number;
  submittedByDisplayName: string;
  assignedToDisplayName?: string;
  adminNotes?: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
}

export interface UpdateTicketAdminRequest {
  status: TicketStatus;
  priority: TicketPriority;
  assignedToUserId?: string | null;
  adminNotes?: string | null;
}

export interface SaveCategoryRequest {
  id: string;
  label: string;
  appliesTo: TicketType[];
  isActive: boolean;
  sortOrder: number;
}
