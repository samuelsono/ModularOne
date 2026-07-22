import { FilterDismissRegular, FilterRegular } from '@fluentui/react-icons';
import {
  Button,
  Menu,
  MenuTrigger,
  MenuList,
  MenuItem,
  MenuItemCheckbox,
  MenuPopover,
  MenuDivider,
  MenuGroupHeader,
  type MenuProps,
} from '@fluentui/react-components';
import type { ComponentType, SVGProps } from 'react';
import { useMemo, useState } from 'react';

export type AppFilterOption = {
  name: string;
  label: string;
  value: string;
  icon?: ComponentType<SVGProps<SVGSVGElement>>;
};

export type AppFilterSelection = Record<string, string[]>;

type AppFiltersProps = {
  filters: AppFilterOption[];
  /** Controlled selection. When omitted, the menu manages selection internally. */
  checkedValues?: AppFilterSelection;
  onFilterChange?: (checkedValues: AppFilterSelection) => void;
};

function countSelected(selection: AppFilterSelection): number {
  return Object.values(selection).reduce((sum, values) => sum + values.length, 0);
}

const AppFilters = ({ filters, checkedValues, onFilterChange }: AppFiltersProps) => {
  const [internalSelection, setInternalSelection] = useState<AppFilterSelection>({});
  const selection = checkedValues ?? internalSelection;
  const selectedCount = useMemo(() => countSelected(selection), [selection]);

  const updateSelection = (next: AppFilterSelection) => {
    if (checkedValues === undefined) {
      setInternalSelection(next);
    }
    onFilterChange?.(next);
  };

  const handleCheckedValueChange: MenuProps['onCheckedValueChange'] = (_, data) => {
    updateSelection({
      ...selection,
      [data.name]: data.checkedItems,
    });
  };

  const clearAll = () => {
    updateSelection({});
  };

  return (
    <Menu
      positioning={{ position: 'below', align: 'end' }}
      checkedValues={selection}
      onCheckedValueChange={handleCheckedValueChange}
    >
      <MenuTrigger disableButtonEnhancement>
        <Button
          icon={selectedCount > 0 ? <FilterDismissRegular /> : <FilterRegular />}
          appearance="subtle"
          aria-label={selectedCount > 0 ? `Filters (${selectedCount} active)` : 'Filter by option'}
        />
      </MenuTrigger>
      <MenuPopover>
        <MenuGroupHeader>Filter by option</MenuGroupHeader>
        <MenuList hasIcons hasCheckmarks>
          {filters.map((filter) => {
            const Icon = filter.icon;
            return (
              <MenuItemCheckbox
                key={`${filter.name}:${filter.value}`}
                icon={Icon ? <Icon /> : undefined}
                name={filter.name}
                value={String(filter.value)}
              >
                {filter.label}
              </MenuItemCheckbox>
            );
          })}
          <MenuDivider />
          <MenuItem disabled={selectedCount === 0} onClick={clearAll}>
            Clear all filters
          </MenuItem>
        </MenuList>
      </MenuPopover>
    </Menu>
  );
};

export default AppFilters;
