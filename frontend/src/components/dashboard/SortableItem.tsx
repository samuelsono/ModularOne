import { ReOrderDotsVerticalRegular } from '@fluentui/react-icons';
import type { ReactNode } from 'react';

interface SortableItemProps {
  id: string;
  index: number;
  label: ReactNode;
  onMove: (fromIndex: number, toIndex: number) => void;
  className?: string;
}

export function SortableItem({ id, index, label, onMove, className }: SortableItemProps) {
  return (
    <div
      draggable
      onDragStart={(event) => {
        event.dataTransfer.setData('text/plain', String(index));
        event.dataTransfer.effectAllowed = 'move';
      }}
      onDragOver={(event) => {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'move';
      }}
      onDrop={(event) => {
        event.preventDefault();
        const fromIndex = Number.parseInt(event.dataTransfer.getData('text/plain'), 10);
        if (!Number.isNaN(fromIndex)) {
          onMove(fromIndex, index);
        }
      }}
      className={`flex items-center gap-2 rounded border border-[#e3e5e7] bg-white px-3 py-2 shadow-sm cursor-grab active:cursor-grabbing ${className ?? ''}`}
      data-sortable-id={id}
    >
      <ReOrderDotsVerticalRegular className="text-neutral-500 shrink-0" />
      <div className="min-w-0 flex-1">{label}</div>
    </div>
  );
}
