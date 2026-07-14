import type { ReportSize } from '../types/report';

export function sizeToGridClass(size: ReportSize): string {
  switch (size) {
    case 'Small':
      return 'col-span-12 md:col-span-3';
    case 'Medium':
      return 'col-span-12 md:col-span-4';
    case 'Large':
      return 'col-span-12 md:col-span-6';
    case 'FullWidth':
    default:
      return 'col-span-12';
  }
}
