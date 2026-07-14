import type {
  Report,
  ReportExecutionResult,
  ReportMetadata,
  ReportsResponse,
  SaveReportRequest,
} from '../types/report';
import { authorizedFetch } from './authService';

export async function getReportMetadata(): Promise<ReportMetadata> {
  return authorizedFetch<ReportMetadata>('/api/reports/metadata');
}

export async function getReports(): Promise<ReportsResponse> {
  return authorizedFetch<ReportsResponse>('/api/reports');
}

export async function getReport(id: string): Promise<Report> {
  return authorizedFetch<Report>(`/api/reports/${encodeURIComponent(id)}`);
}

export async function createReport(request: SaveReportRequest): Promise<Report> {
  return authorizedFetch<Report>('/api/reports', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function updateReport(id: string, request: SaveReportRequest): Promise<Report> {
  return authorizedFetch<Report>(`/api/reports/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export async function deleteReport(id: string): Promise<void> {
  await authorizedFetch<void>(`/api/reports/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export async function previewReport(request: SaveReportRequest): Promise<ReportExecutionResult> {
  return authorizedFetch<ReportExecutionResult>('/api/reports/preview', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export async function executeReport(id: string): Promise<ReportExecutionResult> {
  return authorizedFetch<ReportExecutionResult>(`/api/reports/${encodeURIComponent(id)}/execute`, {
    method: 'POST',
  });
}
