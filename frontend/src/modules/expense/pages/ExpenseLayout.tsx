import { Outlet } from 'react-router-dom';
import { tokens, Subtitle2, Text } from '@fluentui/react-components';

export default function ExpenseLayout() {
  return (
    <div className="flex flex-col h-screen min-h-0 p-3 gap-2" style={{ backgroundColor: tokens.colorNeutralBackground3 }}>
      <div className="flex flex-col">
        <Subtitle2>Expense claims</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3">
          Submit business expenses, track approvals, and review category spend.
        </Text>
      </div>

      <div className="flex-1 min-h-0 overflow-hidden rounded shadow py-3" style={{ backgroundColor: tokens.colorNeutralBackground1 }}>
        <Outlet />
      </div>
    </div>
  );
}
