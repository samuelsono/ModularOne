import { Outlet } from 'react-router-dom';
import { tokens, Subtitle2, Text } from '@fluentui/react-components';

export default function CoreLayout() {
  return (
    <div className="flex flex-col h-screen min-h-0 p-3 gap-2" style={{ backgroundColor: tokens.colorNeutralBackground3 }}>
      <div className="flex flex-col">
        <Subtitle2>Core HR</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3">
          Manage companies, departments, positions, and employee assignments.
        </Text>
      </div>
      <div className="flex-1 min-h-0 overflow-hidden rounded shadow py-3" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <Outlet />
      </div>
    </div>
  );
}
