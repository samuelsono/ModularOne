import { Subtitle2, Text } from '@fluentui/react-components';
import { useActiveApp } from '@platform/shell/ActiveAppContext';

export default function ModulePlaceholder() {
  const { currentModule } = useActiveApp();

  return (
    <div className="flex flex-col items-center justify-center h-full gap-3 p-8 text-center">
      <Subtitle2>{currentModule?.name ?? 'Application'}</Subtitle2>
      <Text className="text-sm text-neutral-foreground-3 max-w-md">
        This module is active in your app launcher. Feature pages for
        {' '}
        {currentModule?.description?.toLowerCase() ?? 'this application'}
        {' '}
        will appear here as they are implemented.
      </Text>
    </div>
  );
}
