import {
  Button,
  MessageBar,
  MessageBarBody,
  Subtitle2,
  Text,
} from '@fluentui/react-components';
import { useEffect, useState } from 'react';

import type { Dashboard } from '../../types/dashboard';
import type { Report, ReportSize } from '../../types/report';
import { getDashboard, reorderDashboardLayout } from '../../services/dashboardService';
import { sizeToGridClass } from '../../utils/gridLayout';
import { moveItem } from '../../utils/reorder';
import { buildSectionTree, type SectionTreeNode } from '../../utils/sectionTree';
import { SortableItem } from './SortableItem';

type LayoutItem =
  | { type: 'report'; id: string; report: Report }
  | { type: 'section'; id: string; section: EditableSection };

interface EditableSection {
  id: string;
  title: string;
  layoutDirection: string;
  size: ReportSize;
  depth: number;
  items: LayoutItem[];
}

interface DashboardLayoutEditorProps {
  dashboardId: string;
  onSaved: () => void;
  onCancel: () => void;
}

export function DashboardLayoutEditor({ dashboardId, onSaved, onCancel }: DashboardLayoutEditorProps) {
  const [sections, setSections] = useState<EditableSection[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      setLoading(true);
      setError(null);

      try {
        const dashboard = await getDashboard(dashboardId);
        if (!cancelled) {
          setSections(mapSections(dashboard));
        }
      } catch {
        if (!cancelled) {
          setError('Unable to load dashboard layout.');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      cancelled = true;
    };
  }, [dashboardId]);

  const moveSection = (fromIndex: number, toIndex: number) => {
    setSections((current) => moveItem(current, fromIndex, toIndex));
  };

  const moveLayoutItem = (sectionPath: number[], fromIndex: number, toIndex: number) => {
    setSections((current) => updateSectionAtPath(current, sectionPath, (section) => ({
      ...section,
      items: moveItem(section.items, fromIndex, toIndex),
    })));
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);

    try {
      await reorderDashboardLayout(dashboardId, {
        sections: flattenSectionsForReorder(sections),
      });
      onSaved();
    } catch {
      setError('Failed to save layout.');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <Text className="px-16 pt-4">Loading layout editor...</Text>;
  }

  return (
    <div className="flex flex-col gap-4 px-16 pt-3 pb-8">
      <div className="flex items-center justify-between gap-3">
        <div className="flex flex-col">
          <Subtitle2>Edit dashboard layout</Subtitle2>
          <Text className="text-sm text-neutral-600">
            Drag reports and nested sections to reorder them together within each section.
          </Text>
        </div>
        <div className="flex gap-2">
          <Button appearance="secondary" onClick={onCancel} disabled={saving}>
            Cancel
          </Button>
          <Button appearance="primary" onClick={() => void handleSave()} disabled={saving}>
            {saving ? 'Saving...' : 'Save layout'}
          </Button>
        </div>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      <div className="flex flex-col gap-4">
        {sections.map((section, sectionIndex) => (
          <SectionEditorNode
            key={section.id}
            section={section}
            path={[sectionIndex]}
            onMoveSection={moveSection}
            onMoveLayoutItem={moveLayoutItem}
          />
        ))}
      </div>
    </div>
  );
}

interface SectionEditorNodeProps {
  section: EditableSection;
  path: number[];
  onMoveSection: (fromIndex: number, toIndex: number) => void;
  onMoveLayoutItem: (sectionPath: number[], fromIndex: number, toIndex: number) => void;
}

function SectionEditorNode({ section, path, onMoveSection, onMoveLayoutItem }: SectionEditorNodeProps) {
  const isRoot = path.length === 1;
  const rootIndex = path[0] ?? 0;

  return (
    <div className="rounded border border-[#d1d5db] bg-neutral-50 p-3">
      {isRoot ? (
        <SortableItem
          id={section.id}
          index={rootIndex}
          onMove={onMoveSection}
          label={(
            <div>
              <Text weight="semibold">{section.title}</Text>
              <Text size={200} className="text-neutral-600">
                {section.layoutDirection} · {section.size === 'FullWidth' ? 'Full Width' : section.size} · level {section.depth}
              </Text>
            </div>
          )}
        />
      ) : (
        <div className="mb-2">
          <Text weight="semibold">{section.title}</Text>
          <Text size={200} className="text-neutral-600">
            {section.layoutDirection} · {section.size === 'FullWidth' ? 'Full Width' : section.size} · level {section.depth}
          </Text>
        </div>
      )}

      <div className={section.layoutDirection === 'Row' ? 'mt-3 grid grid-cols-12 gap-2' : 'mt-3 flex flex-col gap-2'}>
        {section.items.map((item, index) => (
          <LayoutItemNode
            key={item.type === 'report' ? `report-${item.id}` : `section-${item.id}`}
            item={item}
            index={index}
            sectionPath={path}
            layoutDirection={section.layoutDirection}
            onMoveLayoutItem={onMoveLayoutItem}
            onMoveSection={onMoveSection}
          />
        ))}
        {section.items.length === 0 && (
          <Text size={200} className="text-neutral-500 px-2">No reports or nested sections</Text>
        )}
      </div>
    </div>
  );
}

interface LayoutItemNodeProps {
  item: LayoutItem;
  index: number;
  sectionPath: number[];
  layoutDirection: string;
  onMoveLayoutItem: (sectionPath: number[], fromIndex: number, toIndex: number) => void;
  onMoveSection: (fromIndex: number, toIndex: number) => void;
}

function LayoutItemNode({
  item,
  index,
  sectionPath,
  layoutDirection,
  onMoveLayoutItem,
  onMoveSection,
}: LayoutItemNodeProps) {
  if (item.type === 'report') {
    return (
      <SortableItem
        id={item.id}
        index={index}
        onMove={(from, to) => onMoveLayoutItem(sectionPath, from, to)}
        className={layoutDirection === 'Row' ? sizeToGridClass(item.report.size) : 'w-full'}
        label={(
          <div>
            <Text weight="semibold" className="truncate">{item.report.name}</Text>
            <Text size={200} className="text-neutral-600">
              Report · {item.report.reportType} · {item.report.size === 'FullWidth' ? 'Full Width' : item.report.size}
            </Text>
          </div>
        )}
      />
    );
  }

  return (
    <div className={layoutDirection === 'Row' ? sizeToGridClass(item.section.size) : 'w-full'}>
      <SortableItem
        id={item.id}
        index={index}
        onMove={(from, to) => onMoveLayoutItem(sectionPath, from, to)}
        label={(
          <div>
            <Text weight="semibold" className="truncate">{item.section.title}</Text>
            <Text size={200} className="text-neutral-600">
              Section · {item.section.layoutDirection} · {item.section.size === 'FullWidth' ? 'Full Width' : item.section.size}
            </Text>
          </div>
        )}
      />
      <div className="mt-2">
        <SectionEditorNode
          section={item.section}
          path={[...sectionPath, index]}
          onMoveSection={onMoveSection}
          onMoveLayoutItem={onMoveLayoutItem}
        />
      </div>
    </div>
  );
}

function mapSections(dashboard: Dashboard): EditableSection[] {
  return buildSectionTree(dashboard.sections).map(mapTreeNode);
}

function buildLayoutItems(node: SectionTreeNode): LayoutItem[] {
  const entries: Array<{ sortOrder: number; item: LayoutItem }> = [
    ...node.reports.map((report) => ({
      sortOrder: report.sortOrder,
      item: { type: 'report' as const, id: report.id, report },
    })),
    ...node.children.map((child) => ({
      sortOrder: child.sortOrder,
      item: { type: 'section' as const, id: child.id, section: mapTreeNode(child) },
    })),
  ];

  return entries
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .map((entry) => entry.item);
}

function mapTreeNode(node: SectionTreeNode): EditableSection {
  return {
    id: node.id,
    title: node.title ?? `Section ${node.sortOrder}`,
    layoutDirection: node.layoutDirection,
    size: node.size ?? 'FullWidth',
    depth: node.depth,
    items: buildLayoutItems(node),
  };
}

function flattenSectionsForReorder(sections: EditableSection[]) {
  const items: Array<{ sectionId: string; sortOrder: number; items: Array<{ itemType: 'Report' | 'Section'; itemId: string }> }> = [];

  const walk = (nodes: EditableSection[]) => {
    nodes.forEach((section, index) => {
      items.push({
        sectionId: section.id,
        sortOrder: index + 1,
        items: section.items.map((item) => ({
          itemType: item.type === 'report' ? 'Report' : 'Section',
          itemId: item.id,
        })),
      });

      for (const item of section.items) {
        if (item.type === 'section') {
          walk([item.section]);
        }
      }
    });
  };

  walk(sections);
  return items;
}

function updateSectionAtPath(
  sections: EditableSection[],
  path: number[],
  updater: (section: EditableSection) => EditableSection,
): EditableSection[] {
  if (path.length === 0) {
    return sections;
  }

  const [rootIndex, ...rest] = path;

  return sections.map((section, sectionIndex) => {
    if (sectionIndex !== rootIndex) {
      return section;
    }

    if (rest.length === 0) {
      return updater(section);
    }

    const [itemIndex, ...childRest] = rest;
    const layoutItem = section.items[itemIndex];

    if (!layoutItem || layoutItem.type !== 'section') {
      return section;
    }

    return {
      ...section,
      items: section.items.map((item, index) => (
        index === itemIndex && item.type === 'section'
          ? {
              ...item,
              section: updateNestedSection(item.section, childRest, updater),
            }
          : item
      )),
    };
  });
}

function updateNestedSection(
  section: EditableSection,
  path: number[],
  updater: (section: EditableSection) => EditableSection,
): EditableSection {
  if (path.length === 0) {
    return updater(section);
  }

  const [itemIndex, ...rest] = path;
  const layoutItem = section.items[itemIndex];

  if (!layoutItem || layoutItem.type !== 'section') {
    return section;
  }

  return {
    ...section,
    items: section.items.map((item, index) => (
      index === itemIndex && item.type === 'section'
        ? {
            ...item,
            section: updateNestedSection(item.section, rest, updater),
          }
        : item
    )),
  };
}
