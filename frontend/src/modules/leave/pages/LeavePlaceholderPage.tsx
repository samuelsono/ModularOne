import { Subtitle2, Text } from '@fluentui/react-components';

interface LeavePlaceholderPageProps {
  title: string;
  description: string;
}

export default function LeavePlaceholderPage({ title, description }: LeavePlaceholderPageProps) {
  return (
    <div className="flex flex-col items-center justify-center h-full gap-3 p-8 text-center">
      <Subtitle2>{title}</Subtitle2>
      <Text className="text-sm text-neutral-foreground-3 max-w-md">
        {description}
      </Text>
    </div>
  );
}
