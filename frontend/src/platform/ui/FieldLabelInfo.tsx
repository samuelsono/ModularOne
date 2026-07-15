import { InfoLabel, Text } from "@fluentui/react-components";
import type { ReactNode } from "react";

  const FieldLabelInfo = ({ children, text, info }: { children?: ReactNode, text?: ReactNode, info: string }) => (
      <InfoLabel
         className="flex"
         info = {
          <Text className="text-sm text-neutral-foreground-3">
          {info}
        </Text>
         }
      >
        {children ?? text}
      </InfoLabel>
  )

  export default FieldLabelInfo;