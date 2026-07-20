export type ExpenseClaimStatus =
  | 'Draft'
  | 'PendingManager'
  | 'PendingFinance'
  | 'PendingPayment'
  | 'Paid'
  | 'Rejected'
  | 'Cancelled'
  | 'Pending'
  | 'Approved';

export interface ExpenseCategory {
  id: string;
  name: string;
  code: string;
  description: string | null;
  isActive: boolean;
  sortOrder: number;
  requiresReceipt: boolean;
  requiresTravelDetails: boolean;
  paysByKilometer: boolean;
  createdAt: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
}

export interface SaveExpenseCategoryRequest {
  name: string;
  code: string;
  description?: string | null;
  isActive: boolean;
  sortOrder: number;
  requiresReceipt: boolean;
  requiresTravelDetails: boolean;
  paysByKilometer: boolean;
}

export interface ExpenseClaim {
  id: string;
  requesterUserId: string;
  requesterDisplayName: string;
  managerUserId: string;
  managerDisplayName?: string | null;
  categoryId: string;
  categoryName: string;
  categoryCode: string;
  requiresReceipt: boolean;
  requiresTravelDetails: boolean;
  paysByKilometer: boolean;
  expenseDate: string;
  description: string;
  notes: string | null;
  amount: number;
  kilometersTravelled?: number | null;
  travelStartPoint?: string | null;
  travelDestination?: string | null;
  travelWaypoints?: string[] | null;
  mileageRatePerKilometer?: number | null;
  hasReceipt: boolean;
  receiptFileName?: string | null;
  receiptUploadedAt?: string | null;
  currency: string;
  status: ExpenseClaimStatus | string;
  createdAt: string;
  createdByUserId?: string | null;
  createdByDisplayName?: string | null;
  updatedAt?: string | null;
  updatedByUserId?: string | null;
  updatedByDisplayName?: string | null;
  submittedAt: string | null;
  decidedAt: string | null;
  paidAt: string | null;
}

export interface CreateExpenseClaimRequest {
  categoryId: string;
  expenseDate: string;
  description: string;
  notes?: string | null;
  amount: number;
  currency?: string | null;
  kilometersTravelled?: number | null;
  travelStartPoint?: string | null;
  travelDestination?: string | null;
  travelWaypoints?: string[] | null;
}

export interface UpdateExpenseClaimRequest extends CreateExpenseClaimRequest {}

export interface ExpenseSettings {
  kilometerRate: number;
  updatedAt: string | null;
}

export interface UpdateExpenseSettingsRequest {
  kilometerRate: number;
}

export interface ApprovalDecisionRequest {
  approve: boolean;
  notes?: string | null;
}

export const EXPENSE_STATUS_FILTERS = [
  'All',
  'Draft',
  'PendingManager',
  'PendingFinance',
  'PendingPayment',
  'Paid',
  'Rejected',
  'Cancelled',
] as const;

export function expenseStatusLabel(status: string): string {
  switch (status) {
    case 'Draft':
      return 'Draft';
    case 'PendingManager':
      return 'Pending manager';
    case 'PendingFinance':
      return 'Pending finance';
    case 'PendingPayment':
      return 'Pending payment';
    case 'Paid':
      return 'Paid';
    case 'Pending':
      return 'Pending manager';
    case 'Approved':
      return 'Pending payment';
    default:
      return status;
  }
}

export function isEditableExpenseClaim(status: string): boolean {
  return status === 'Draft';
}

export function isCancellableExpenseClaim(status: string): boolean {
  return status === 'Draft' || status === 'PendingManager';
}

export function isApprovalStage(status: string): boolean {
  return status === 'PendingManager' || status === 'PendingFinance';
}

export interface ExpenseReportCount {
  label: string;
  count: number;
}

export interface ExpenseReportAmount {
  label: string;
  count: number;
  amount: number;
}

export interface ExpenseReportSummary {
  pendingCount: number;
  pendingAmount: number;
  approvedYtdAmount: number;
  pendingPaymentAmount: number;
  paidYtdAmount: number;
  totalSubmittedYtd: number;
  byCategory: ExpenseReportAmount[];
  byStatus: ExpenseReportCount[];
}

export interface ExpenseCategoryBalance {
  categoryId: string;
  categoryName: string;
  categoryCode: string;
  pendingAmount: number;
  pendingPaymentAmount: number;
  paidAmount: number;
  rejectedAmount: number;
  cancelledAmount: number;
  draftAmount: number;
  totalSubmitted: number;
}
