import type { DashboardRender, DashboardSectionRender } from '@modules/reporting/types/dashboard';
import type { Report } from '@modules/reporting/types/report';
import { matchesSearchQuery } from '@platform/search/searchText';

export function filterReports(reports: Report[], query: string): Report[] {
  const normalized = query.trim();
  if (!normalized) return reports;
  return reports.filter((report) => {
    const placementLabels = (report.placements ?? []).flatMap((placement) => [
      placement.dashboardName, placement.sectionTitle,
    ]);
    return matchesSearchQuery(normalized, [
      report.name, report.description, report.reportType, report.targetTable, ...placementLabels,
    ]);
  });
}

export function filterDashboardSummaries<T extends { name: string; description?: string | null }>(
  items: T[], query: string,
): T[] {
  const normalized = query.trim();
  if (!normalized) return items;
  return items.filter((item) => matchesSearchQuery(normalized, [item.name, item.description]));
}

export function filterDashboardSections<T extends {
  title?: string | null; subtitle?: string | null; layoutDirection: string;
}>(sections: T[], query: string): T[] {
  const normalized = query.trim();
  if (!normalized) return sections;
  return sections.filter((section) => matchesSearchQuery(normalized, [
    section.title, section.subtitle, section.layoutDirection,
  ]));
}

export function filterDashboardRender(dashboard: DashboardRender, query: string): DashboardRender {
  const normalized = query.trim();
  if (!normalized) return dashboard;
  return {
    ...dashboard,
    sections: dashboard.sections
      .map((section) => filterSectionRender(section, normalized))
      .filter((section) => sectionHasVisibleContent(section)),
  };
}

function filterSectionRender(section: DashboardSectionRender, query: string): DashboardSectionRender {
  const filteredReports = section.reports.filter((item) => matchesSearchQuery(query, [
    item.report.name, item.report.description, item.report.reportType,
  ]));
  const filteredChildren = section.childSections
    .map((child) => filterSectionRender(child, query))
    .filter((child) => sectionHasVisibleContent(child));
  const sectionMatches = matchesSearchQuery(query, [section.title, section.subtitle]);
  return {
    ...section,
    reports: sectionMatches ? section.reports : filteredReports,
    childSections: sectionMatches ? section.childSections : filteredChildren,
  };
}

function sectionHasVisibleContent(section: DashboardSectionRender): boolean {
  return section.reports.length > 0
    || section.childSections.some((child) => sectionHasVisibleContent(child))
    || Boolean(section.title)
    || Boolean(section.subtitle);
}
