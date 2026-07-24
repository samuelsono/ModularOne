import { Button, Dropdown, Option, Text } from "@fluentui/react-components";
import {
  ChevronDoubleLeftRegular,
  ChevronDoubleRightRegular,
  ChevronLeftRegular,
  ChevronRightRegular,
} from "@fluentui/react-icons";

const DEFAULT_PAGE_SIZES = [10, 15, 25, 50];

export interface AppPaginationProps {
  /** Current page (1-based). */
  page: number;
  /** Total number of pages. */
  totalPages: number;
  /** Total number of items across all pages. */
  totalItems: number;
  /** Index of the first item shown on the current page (1-based). */
  rangeStart: number;
  /** Index of the last item shown on the current page (1-based). */
  rangeEnd: number;
  /** Current page size. */
  pageSize: number;
  /** Called when the user navigates to a different page. */
  onPageChange: (page: number) => void;
  /** Called when the user selects a different page size. */
  onPageSizeChange: (size: number) => void;
  /** Selectable page sizes. Defaults to [10, 15, 25, 50]. */
  pageSizeOptions?: readonly number[];
  className?: string;
}

const AppPagination = ({
  page,
  totalPages,
  totalItems,
  rangeStart,
  rangeEnd,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = DEFAULT_PAGE_SIZES,
  className,
}: AppPaginationProps) => {
  return (
    <div className={`flex flex-wrap items-center justify-between gap-3 px-6 ${className ?? ""}`}>
      <div className="flex items-center gap-2">
        <div className="flex items-center gap-2">
            <Text className="text-sm text-neutral-foreground-3">Rows per page</Text>
            <Dropdown
              style={{ width: 72, minWidth: 72 }}
              className="shrink-0"
              value={String(pageSize)}
              selectedOptions={[String(pageSize)]}
              onOptionSelect={(_, data) => {
                const nextSize = Number(data.optionValue);
                if (pageSizeOptions.includes(nextSize)) {
                  onPageSizeChange(nextSize);
                }
              }}
            >
              {pageSizeOptions.map((size) => (
                <Option key={size} value={String(size)}>
                  {String(size)}
                </Option>
              ))}
            </Dropdown>
          </div>
        
          <Text className="text-sm text-neutral-foreground-3">
            Showing {rangeStart}–{rangeEnd} of {totalItems}
          </Text>
      </div>


      <div className="flex items-center gap-1">
        <Button
          appearance="outline"
          icon={<ChevronDoubleLeftRegular />}
          aria-label="First page"
          disabled={page <= 1}
          onClick={() => onPageChange(1)}
        />
        <Button
          appearance="outline"
          icon={<ChevronLeftRegular />}
          aria-label="Previous page"
          disabled={page <= 1}
          onClick={() => onPageChange(Math.max(1, page - 1))}
        />
        <Text className="text-sm text-neutral-foreground-3 px-2">
          Page {page} of {totalPages}
        </Text>
        <Button
          appearance="outline"
          icon={<ChevronRightRegular />}
          aria-label="Next page"
          disabled={page >= totalPages}
          onClick={() => onPageChange(Math.min(totalPages, page + 1))}
        />
        <Button
          appearance="outline"
          icon={<ChevronDoubleRightRegular />}
          aria-label="Last page"
          disabled={page >= totalPages}
          onClick={() => onPageChange(totalPages)}
        />
      </div>

      
    </div>
  );
};

export default AppPagination;