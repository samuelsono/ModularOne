import { Button, Spinner } from "@fluentui/react-components";
import { ArrowClockwiseRegular } from "@fluentui/react-icons";
import { useState } from "react";

type DataReloadProps = {
  onReload: () => Promise<void>;
};

const DataReload: React.FC<DataReloadProps> = ({ onReload }) => {
  const [isReloading, setIsReloading] = useState(false);

  const handleReload = async () => {
    setIsReloading(true);
    try {
      await onReload();
    } finally {
      setIsReloading(false);
    }
  };

  return (
    <div className="flex items-center gap-2">
      <Button
        appearance="outline"
        onClick={handleReload}
        disabled={isReloading}
        icon={isReloading ? <Spinner size={"extra-tiny"} /> : <ArrowClockwiseRegular />}
      >
      </Button>
    </div>
  );
};

export default DataReload;