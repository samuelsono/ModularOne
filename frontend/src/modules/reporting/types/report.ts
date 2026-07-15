export type ReportType = 'MetricCard' | 'Column' | 'Pie' | 'Donut' | 'Line' | 'ApiTable' | 'Map';
export type ReportSize = 'Small' | 'Medium' | 'Large' | 'FullWidth';
export type LayoutDirection = 'Row' | 'Column';

export interface ReportFilter {
  field: string;
  operator: string;
  value: string;
}

export interface ReportPlacement {
  id: string;
  sectionId: string;
  dashboardId?: string | null;
  dashboardName?: string | null;
  sectionTitle?: string | null;
  sortOrder: number;
  size: ReportSize;
  isVisible: boolean;
}

export interface Report {
  id: string;
  sectionId?: string | null;
  dashboardId?: string | null;
  dashboardName?: string | null;
  sectionTitle?: string | null;
  name: string;
  description?: string | null;
  reportType: ReportType;
  size: ReportSize;
  sortOrder: number;
  isVisible: boolean;
  targetTable: string;
  aggregateFunction: string;
  aggregateField?: string | null;
  groupByColumns: string[];
  filters: ReportFilter[];
  comparisonEnabled: boolean;
  chartOptionsJson?: string | null;
  placements: ReportPlacement[];
  updatedAt: string;
}

export interface ReportsResponse {
  items: Report[];
  total: number;
}

export interface SaveReportPlacementRequest {
  sectionId: string;
  sortOrder: number;
  size: ReportSize;
  isVisible: boolean;
}

export interface SaveReportRequest {
  name: string;
  description?: string | null;
  reportType: ReportType;
  size: ReportSize;
  sortOrder: number;
  isVisible: boolean;
  targetTable: string;
  aggregateFunction: string;
  aggregateField?: string | null;
  groupByColumns?: string[];
  filters?: ReportFilter[];
  comparisonEnabled: boolean;
  chartOptionsJson?: string | null;
  sectionId?: string | null;
  placements?: SaveReportPlacementRequest[];
}

export interface ReportMetric {
  value: number;
  comparisonValue?: number | null;
  changePercent?: number | null;
}

export interface ReportExecutionResult {
  reportId: string;
  reportType: ReportType;
  name: string;
  columns: string[];
  rows: (string | number | null)[][];
  metric?: ReportMetric | null;
}

export interface ReportWithData {
  report: Report;
  data: ReportExecutionResult;
}

export interface ReportColumnMetadata {
  name: string;
  label: string;
  kind: string;
  isGroupable: boolean;
  isAggregatable: boolean;
}

export interface ReportTableMetadata {
  name: string;
  label: string;
  columns: ReportColumnMetadata[];
  allowedAggregates: string[];
}

export interface ReportTypeRule {
  name: string;
  maxGroupByColumns: number;
}

export interface ReportMetadata {
  tables: ReportTableMetadata[];
  reportTypes: string[];
  reportSizes: string[];
  layoutDirections: string[];
  aggregateFunctions: string[];
  reportTypeRules: ReportTypeRule[];
  apiResources: string[];
}
