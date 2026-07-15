import { Body1, Button, Card, CardHeader, Text } from '@fluentui/react-components';

export type OtherType = {
  label: string;
  value: string | number;
};

export type SummaryCardProps = {
  label: string;
  value: string | number;
  other?: OtherType;
  description?: string;
  leaveTypeColor?: string;
  onSelectTarget?: () => void;
  canAdjust?: boolean;
};

function SummaryCard({
  label,
  value,
  description,
  leaveTypeColor,
  other,
  onSelectTarget,
  canAdjust,
}: SummaryCardProps) {
  return (
    <Card className="h-full">
      <CardHeader
        header={
          <Text weight="semibold">
            {leaveTypeColor ? (
              <span
                className="inline-block w-3 h-3 rounded-full shrink-0 mr-2"
                style={{ backgroundColor: leaveTypeColor }}
              />
            ) : null}
            {label}
          </Text>
        }
        description={<Body1 className="font-thin!">{description}</Body1>}
        action={
          <>
            {canAdjust && (
              <Button size="small" appearance="subtle" onClick={onSelectTarget}>
                Adjust
              </Button>
            )}
          </>
        }
      />
      <div className="flex items-end justify-between">
        <p className="text-3xl text-left font-thin">{value}</p>
        {other && (
          <div className="flex flex-col items-end font-thin">
            <span className="text-xl">{other.value}</span>
            <span className="font-bold text-xs">{other.label}</span>
          </div>
        )}
      </div>
    </Card>
  );
}

export default SummaryCard;
