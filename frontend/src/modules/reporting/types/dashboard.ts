import type { Report, ReportWithData, ReportSize } from './report';

export const MAX_SECTION_DEPTH = 3;

export interface DashboardSection {
  id: string;
  dashboardId: string;
  parentSectionId?: string | null;
  depth: number;
  title?: string | null;
  subtitle?: string | null;
  layoutDirection: 'Row' | 'Column';
  size: ReportSize;
  sortOrder: number;
  reports: Report[];
  updatedAt: string;
}

export interface Dashboard {
  id: string;
  name: string;
  description?: string | null;
  isDefault: boolean;
  sortOrder: number;
  sections: DashboardSection[];
  updatedAt: string;
}

export interface DashboardSummary {
  id: string;
  name: string;
  description?: string | null;
  isDefault: boolean;
  sortOrder: number;
  sectionCount: number;
  reportCount: number;
  updatedAt: string;
}

export interface DashboardsResponse {
  items: DashboardSummary[];
  total: number;
}

export interface DashboardSectionRender {
  id: string;
  dashboardId: string;
  parentSectionId?: string | null;
  depth: number;
  title?: string | null;
  subtitle?: string | null;
  layoutDirection: 'Row' | 'Column';
  size: ReportSize;
  sortOrder: number;
  reports: ReportWithData[];
  childSections: DashboardSectionRender[];
  updatedAt: string;
}

export interface DashboardRender {
  id: string;
  name: string;
  description?: string | null;
  isDefault: boolean;
  sortOrder: number;
  sections: DashboardSectionRender[];
  updatedAt: string;
}

export interface ReorderLayoutItem {
  itemType: 'Report' | 'Section';
  itemId: string;
}

export interface ReorderSectionLayout {
  sectionId: string;
  sortOrder: number;
  items: ReorderLayoutItem[];
}

export interface ReorderDashboardLayoutRequest {
  sections: ReorderSectionLayout[];
}

export interface SaveDashboardRequest {
  name: string;
  description?: string | null;
  isDefault: boolean;
  sortOrder: number;
}

export interface SaveDashboardSectionRequest {
  title?: string | null;
  subtitle?: string | null;
  layoutDirection: 'Row' | 'Column';
  size: ReportSize;
  sortOrder: number;
  parentSectionId?: string | null;
}
