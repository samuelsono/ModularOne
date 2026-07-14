import { Text } from '@fluentui/react-components';
import type { OrgChartNode } from '../../types/user';

function OrgChartBranch({ node, depth = 0 }: { node: OrgChartNode; depth?: number }) {
  return (
    <li className="list-none">
      <div
        className="rounded border border-[#e3e5e7] px-3 py-2 bg-white"
        style={{ marginLeft: depth * 16 }}
      >
        <Text weight="semibold" block>{node.displayName}</Text>
        {(node.jobTitle || node.department) && (
          <Text size={200} className="text-neutral-foreground-3 block">
            {[node.jobTitle, node.department].filter(Boolean).join(' · ')}
          </Text>
        )}
      </div>
      {node.reports.length > 0 && (
        <ul className="mt-2 flex flex-col gap-2">
          {node.reports.map((report: OrgChartNode) => (
            <OrgChartBranch key={report.id} node={report} depth={depth + 1} />
          ))}
        </ul>
      )}
    </li>
  );
}

export function UserOrgChart({ tree }: { tree: OrgChartNode }) {
  return (
    <div className="flex flex-col gap-2">
      <Text weight="semibold">Org chart</Text>
      <ul>
        <OrgChartBranch node={tree} />
      </ul>
    </div>
  );
}
