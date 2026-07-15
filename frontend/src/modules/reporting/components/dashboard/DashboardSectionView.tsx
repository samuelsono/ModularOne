import { Body1, Subtitle2 } from '@fluentui/react-components';

import type { DashboardSectionRender } from '@modules/reporting/types/dashboard';
import type { ReportSize } from '@modules/reporting/types/report';
import { mergeSectionContent } from '@modules/reporting/utils/sectionLayout';
import { sizeToGridClass } from '@modules/reporting/utils/gridLayout';
import { ReportWidget } from '../reports/ReportWidget';

interface DashboardSectionViewProps {
  section: DashboardSectionRender;
  /** When true, the section is placed inside a parent grid and should not add its own outer column span. */
  nested?: boolean;
  onReportUpdated?: () => void;
}

export function DashboardSectionView({ section, nested = false, onReportUpdated }: DashboardSectionViewProps) {
  const isRow = section.layoutDirection === 'Row';
  const sectionSize = (section.size ?? 'FullWidth') as ReportSize;
  const contentItems = mergeSectionContent(section);
  const stretchColumnChildren = nested && !isRow;
  const fillHeight = nested || isRow;

  const body = (
    <div className={`flex flex-col gap-2 min-w-0${fillHeight ? ' h-full flex-1' : ''}`}>
      {(section.title || section.subtitle) && (
        <div className="flex flex-col shrink-0">
          {section.title && <Subtitle2>{section.title}</Subtitle2>}
          {section.subtitle && <Body1 className="font-thin! text-neutral-600">{section.subtitle}</Body1>}
        </div>
      )}

      {contentItems.length > 0 && (
        isRow ? (
          <div className="grid grid-cols-12 items-stretch gap-3 h-full flex-1 min-h-0">
            {contentItems.map((item) => {
              if (item.kind === 'report') {
                return (
                  <div
                    key={`${section.id}-report-${item.report.report.id}`}
                    className={`${sizeToGridClass(item.report.report.size)} flex min-h-0 flex-col`}
                  >
                    <ReportWidget
                      item={item.report}
                      sectionId={section.id}
                      onUpdated={onReportUpdated}
                    />
                  </div>
                );
              }

              return (
                <div
                  key={`${section.id}-section-${item.section.id}`}
                  className={`${sizeToGridClass((item.section.size ?? 'FullWidth') as ReportSize)} flex min-h-0 flex-col`}
                >
                  <DashboardSectionView section={item.section} nested onReportUpdated={onReportUpdated} />
                </div>
              );
            })}
          </div>
        ) : (
          <div
            className={
              stretchColumnChildren
                ? 'flex flex-col gap-3 min-w-0 h-full flex-1 min-h-0'
                : 'flex flex-col gap-3 min-w-0'
            }
          >
            {contentItems.map((item) => {
              const childClass = stretchColumnChildren
                ? 'flex flex-1 min-h-0 flex-col w-full'
                : 'w-full min-w-0';

              if (item.kind === 'report') {
                return (
                  <div key={`${section.id}-report-${item.report.report.id}`} className={childClass}>
                    <ReportWidget
                      item={item.report}
                      sectionId={section.id}
                      onUpdated={onReportUpdated}
                    />
                  </div>
                );
              }

              return (
                <div key={`${section.id}-section-${item.section.id}`} className={childClass}>
                  <DashboardSectionView section={item.section} nested onReportUpdated={onReportUpdated} />
                </div>
              );
            })}
          </div>
        )
      )}
    </div>
  );

  if (nested) {
    return body;
  }

  return (
    <div className={`${sizeToGridClass(sectionSize)}${isRow ? ' h-full' : ''}`}>
      {body}
    </div>
  );
}
