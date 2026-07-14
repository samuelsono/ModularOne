import type { DashboardSectionRender } from '../types/dashboard';
import type { ReportWithData } from '../types/report';

export type SectionContentItem =
  | { kind: 'report'; sortOrder: number; report: ReportWithData }
  | { kind: 'section'; sortOrder: number; section: DashboardSectionRender };

export function mergeSectionContent(section: DashboardSectionRender): SectionContentItem[] {
  return [
    ...section.reports.map((item) => ({
      kind: 'report' as const,
      sortOrder: item.report.sortOrder,
      report: item,
    })),
    ...section.childSections.map((child) => ({
      kind: 'section' as const,
      sortOrder: child.sortOrder,
      section: child,
    })),
  ].sort((left, right) => left.sortOrder - right.sortOrder);
}
