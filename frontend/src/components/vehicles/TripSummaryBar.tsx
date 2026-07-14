import { useMemo } from 'react';
import type { Trip } from '../../types/vehicle';
import { getTripSummaryMetrics } from './tripEventUtils';

interface TripSummaryBarProps {
  trip: Trip;
}

const SUMMARY_ITEMS = [
  { key: 'stop', label: 'Stop' },
  { key: 'kilometers', label: 'Kilometers' },
  { key: 'driving', label: 'Driving' },
  { key: 'idling', label: 'Idling' },
  { key: 'ignition', label: 'Ignition' },
] as const;

export function TripSummaryBar({ trip }: TripSummaryBarProps) {
  const metrics = useMemo(() => getTripSummaryMetrics(trip), [trip]);

  return (
    <div className="flex border-t border-[#e3e5e7] bg-white">
      {SUMMARY_ITEMS.map((item, index) => (
        <div
          key={item.key}
          className={`flex min-w-0 flex-1 flex-col items-center justify-center px-2 py-3 ${
            index < SUMMARY_ITEMS.length - 1 ? 'border-r border-[#e3e5e7]' : ''
          }`}
        >
          <span className="text-base font-medium tabular-nums text-neutral-foreground-1">
            {metrics[item.key]}
          </span>
          <span className="mt-0.5 text-xs text-neutral-foreground-3">
            {item.label}
          </span>
        </div>
      ))}
    </div>
  );
}
