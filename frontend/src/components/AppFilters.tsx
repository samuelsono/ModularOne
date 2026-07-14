import { FilterDismissRegular, FilterRegular } from "@fluentui/react-icons";
import {
  Button,
  Menu,
  MenuTrigger,
  MenuList,
  MenuItem,
  MenuItemCheckbox,
  MenuPopover,
  Divider,
  MenuDivider,
  MenuGroupHeader,
} from "@fluentui/react-components";
import { CutRegular, CutFilled, bundleIcon } from "@fluentui/react-icons";
import React from "react";

const CutIcon = bundleIcon(CutFilled, CutRegular);


const AppFilters = ({ filters, onFilterChange }: { filters: any; onFilterChange: (filterName: string, value: any) => void }) => {
  const handleFilterChange = (filterName: string, value: any) => {
    onFilterChange(filterName, value);
  };

  const [selectedFilters, setSelectedFilters] = React.useState<{ [key: string]: any }>({});

  const handleMenuItemClick = (filterName: string, value: any) => {
    const newSelectedFilters = { ...selectedFilters };
    if (newSelectedFilters[filterName] === value) {
      delete newSelectedFilters[filterName];
    } else {
      newSelectedFilters[filterName] = value;
    }
    setSelectedFilters(newSelectedFilters);
    handleFilterChange(filterName, value);
  };

  return (
     <Menu >
    <MenuTrigger disableButtonEnhancement>
        <Button icon={Object.keys(selectedFilters).length > 0 ? <FilterDismissRegular /> : <FilterRegular />} appearance="subtle" />
    </MenuTrigger>
    <MenuPopover positioning={{ position: "after", align: "start" }}>
      <MenuGroupHeader>Filter by option</MenuGroupHeader>
      <MenuList hasIcons hasCheckmarks>
        { filters.map((filter:any, index:number) => (
          <MenuItemCheckbox key={index} icon={filter.icon ? <filter.icon /> : undefined} name={filter.name} value={filter.value} checked={selectedFilters[filter.name] === filter.value} onClick={() => handleMenuItemClick(filter.name, filter.value)}>
            {filter.label}
          </MenuItemCheckbox>
        )) }
        <MenuDivider />
        </MenuList>
        <MenuList>
            <MenuItem  onClick={() => setSelectedFilters({})}>Clear all Filters</MenuItem>
        </MenuList>
    </MenuPopover>
  </Menu>

  );
};
    

export default AppFilters;