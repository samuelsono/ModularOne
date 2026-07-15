import type { DashboardSection } from '@modules/reporting/types/dashboard';
import { MAX_SECTION_DEPTH } from '@modules/reporting/types/dashboard';

export interface SectionTreeNode extends DashboardSection {
  children: SectionTreeNode[];
}

export function buildSectionTree(sections: DashboardSection[]): SectionTreeNode[] {
  const nodes = new Map<string, SectionTreeNode>();

  for (const section of sections) {
    nodes.set(section.id, { ...section, children: [] });
  }

  const roots: SectionTreeNode[] = [];

  for (const section of sections) {
    const node = nodes.get(section.id);
    if (!node) {
      continue;
    }

    if (section.parentSectionId && nodes.has(section.parentSectionId)) {
      nodes.get(section.parentSectionId)?.children.push(node);
    } else {
      roots.push(node);
    }
  }

  const sortNodes = (items: SectionTreeNode[]) => {
    items.sort((a, b) => a.sortOrder - b.sortOrder || (a.title ?? '').localeCompare(b.title ?? ''));
    items.forEach((item) => sortNodes(item.children));
  };

  sortNodes(roots);
  return roots;
}

export function flattenSectionTree(nodes: SectionTreeNode[], depth = 1): Array<SectionTreeNode & { treeDepth: number }> {
  return nodes.flatMap((node) => [
    { ...node, treeDepth: depth },
    ...flattenSectionTree(node.children, depth + 1),
  ]);
}

export function getSectionLabel(section: Pick<DashboardSection, 'title' | 'sortOrder'>, depth?: number): string {
  const label = section.title ?? `Section ${section.sortOrder}`;
  return depth && depth > 1 ? `${'— '.repeat(depth - 1)}${label}` : label;
}

export function getEligibleParentSections(
  sections: DashboardSection[],
  sectionId?: string,
): DashboardSection[] {
  return sections.filter((candidate) => {
    if (sectionId && candidate.id === sectionId) {
      return false;
    }

    if (sectionId && isDescendant(sectionId, candidate.id, sections)) {
      return false;
    }

    return candidate.depth < MAX_SECTION_DEPTH;
  });
}

export function isDescendant(ancestorId: string, sectionId: string, sections: DashboardSection[]): boolean {
  const sectionsById = new Map(sections.map((section) => [section.id, section]));
  let current = sectionsById.get(sectionId);

  while (current?.parentSectionId) {
    if (current.parentSectionId === ancestorId) {
      return true;
    }

    current = sectionsById.get(current.parentSectionId);
  }

  return false;
}

export function getNextSiblingSortOrder(sections: DashboardSection[], parentSectionId?: string | null): number {
  const siblings = sections.filter((section) => (section.parentSectionId ?? null) === (parentSectionId ?? null));
  return siblings.length > 0 ? Math.max(...siblings.map((section) => section.sortOrder)) + 1 : 1;
}
