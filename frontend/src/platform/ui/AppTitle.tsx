import { Subtitle2, Text } from "@fluentui/react-components";

const AppTitle = ({ title, subtitle }: { title: string, subtitle?: string }) => {
    return (
        <div className='flex flex-col'>
        <Subtitle2>{title}</Subtitle2>
        {subtitle && (
          <Text className="text-sm text-neutral-foreground-3 3xl:max-w-[26vw]">
            {subtitle}
          </Text>
        )}
      </div>
    )
}

export default AppTitle;