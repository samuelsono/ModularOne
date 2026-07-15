import { Spinner } from "@fluentui/react-components";

const EmptyListOrTable = ({ isLoading, isEmpty, canWrite = true, icon: Icon, message, children }: { isLoading: boolean; isEmpty: boolean; canWrite?: boolean; icon: React.ElementType; message: string  ; children: React.ReactNode }) => {
  if (isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <Spinner label="Loading..." />
      </div>
    );
  }

  if (isEmpty) {
    return (<div className="h-[70vh] max-h-full w-full flex flex-col gap-3 items-center justify-center p-6 text-sm text-neutral-foreground-3">
                           <Icon className='size-26 text-gray-300' />
                            {message}
                            {canWrite ? (
                            <>{children}</>
                            ) : null}
              </div>)
  }

};

export default EmptyListOrTable;