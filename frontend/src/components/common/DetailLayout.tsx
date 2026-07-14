import type { ReactNode } from 'react';

export function DetailRow({
  icon,
  label,
  value,
}: {
  icon: ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div className="grid grid-cols-[20px_140px_1fr] items-center gap-3 border-b border-[#f0f0f0] py-2.5 last:border-b-0">
      <span className="text-neutral-foreground-3">{icon}</span>
      <span className="text-sm text-neutral-foreground-3">{label}</span>
      <span className="text-sm text-neutral-foreground-1">{value}</span>
    </div>
  );
}

export function DetailSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="py-4">
      <h3 className="mb-2 text-base font-semibold text-neutral-foreground-1">{title}</h3>
      <div>{children}</div>
    </section>
  );
}

export const detailPanelShellClassName =
  'flex max-h-[90vh] w-full max-w-[640px] flex-col overflow-hidden rounded border border-[#e3e5e7] bg-white shadow-sm';

export const detailPanelHeaderClassName = 'border-b border-[#e3e5e7] px-6 pb-4 pt-6';

export const detailPanelBodyClassName = 'flex-1 overflow-y-auto px-6';

export const detailPanelFooterClassName = 'border-t border-[#e3e5e7] px-6 py-4';
