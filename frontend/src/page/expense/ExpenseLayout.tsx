import { Outlet } from 'react-router-dom';
import { Subtitle2, Text } from '@fluentui/react-components';

export default function ExpenseLayout() {
  return (
    <div className="flex flex-col h-screen min-h-0 p-3 gap-2">
      <div className="flex flex-col">
        <Subtitle2>Expense claims</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3">
          Submit business expenses, track approvals, and review category spend.
        </Text>
      </div>

      <div className="flex-1 min-h-0 overflow-hidden bg-white rounded shadow py-3">
        <Outlet />
      </div>
    </div>
  );
}
