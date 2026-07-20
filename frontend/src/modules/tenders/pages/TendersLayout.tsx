import { Outlet } from 'react-router-dom';
import { Subtitle2, Text } from '@fluentui/react-components';

export default function TendersLayout() {
  return (
    <div className="flex flex-col h-screen min-h-0 p-3 gap-2">
      <div className="flex flex-col">
        <Subtitle2>Tender management</Subtitle2>
        <Text className="text-sm text-neutral-foreground-3">
          Watch source URLs for keyword matches, document links, and scrape runs.
        </Text>
      </div>
      <div className="flex-1 min-h-0 overflow-hidden bg-white rounded shadow py-3">
        <Outlet />
      </div>
    </div>
  );
}
