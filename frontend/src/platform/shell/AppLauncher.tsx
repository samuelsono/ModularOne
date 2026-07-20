import React from "react";
import {
  makeStyles,
  Button,
  Popover,
  PopoverSurface,
  PopoverTrigger,
  SearchBox,
  Card,
  CardPreview,
  CardHeader,
  Caption1,
  Text,
} from "@fluentui/react-components";
import type { PopoverProps } from "@fluentui/react-components";
import { GridDotsRegular } from "@fluentui/react-icons/fonts/grid-dots";
import { useActiveApp } from '@platform/shell/ActiveAppContext';
import type { AppModuleDefinition } from '@platform/permissions/apps';

const useStyles = makeStyles({
  icon18: { fontSize: "18px" },
  horizontalCardImage: {
    width: "64px",
    height: "64px",
  },
  card: {
    width: "100%",
    maxWidth: "400px",
    height: "fit-content",
    cursor: "pointer",
  },
  cardSelected: {
    outlineWidth: "2px",
    outlineStyle: "solid",
    outlineColor: "var(--colorBrandStroke1)",
  },
  caption: {
    color: "rgba(0, 0, 0, 0.6)",
  },
});

interface AppLauncherContentProps {
  onNavigate?: () => void;
}

const AppLauncherContent = ({ onNavigate }: AppLauncherContentProps) => {
  const {
    visibleModules,
    isDefaultModule,
    selectModule,
    currentModuleSlug,
  } = useActiveApp();
  const [search, setSearch] = React.useState("");

  const filteredModules = React.useMemo(() => {
    const normalized = search.trim().toLowerCase();
    if (!normalized) {
      return visibleModules;
    }

    return visibleModules.filter(
      (module) =>
        module.name.toLowerCase().includes(normalized)
        || module.description.toLowerCase().includes(normalized),
    );
  }, [search, visibleModules]);

  return (
    <div className="p-4">
      <SearchBox
        placeholder="Search applications..."
        className="min-w-3xl"
        value={search}
        onChange={(_event, data) => setSearch(data.value)}
      />
      <div className="grid grid-cols-2 gap-2 gap-5 py-10">
        {filteredModules.length === 0 ? (
          <Text className="col-span-2 text-sm text-neutral-foreground-3">
            No applications are available for your account.
          </Text>
        ) : (
          filteredModules.map((app) => (
            <AppLaunchCard
              key={app.slug}
              app={app}
              isDefault={isDefaultModule(app.slug)}
              isCurrent={currentModuleSlug === app.slug}
              onSelect={() => {
                selectModule(app.slug);
                onNavigate?.();
              }}
            />
          ))
        )}
      </div>
    </div>
  );
};

export const AppLauncher = (props?: PopoverProps) => {
  const styles = useStyles();
  const [open, setOpen] = React.useState(false);

  return (
    <Popover
      {...props}
      open={open}
      onOpenChange={(_event, data) => setOpen(data.open)}
    >
      <PopoverTrigger disableButtonEnhancement>
        <Button
          appearance="transparent"
          icon={<GridDotsRegular className={`${styles.icon18}`} />}
          style={{ color: 'var(--colorBrandBackgroundInverted)' }}
        />
      </PopoverTrigger>

      <PopoverSurface tabIndex={-1} className="mt-3!">
        <AppLauncherContent onNavigate={() => setOpen(false)} />
      </PopoverSurface>
    </Popover>
  );
};

interface AppLaunchCardProps {
  app: AppModuleDefinition;
  isDefault: boolean;
  isCurrent: boolean;
  onSelect: () => void;
}

const AppLaunchCard = ({
  app,
  isDefault,
  isCurrent,
  onSelect,
}: AppLaunchCardProps) => {
  const styles = useStyles();

  return (
    <section>
      <Card
        onClick={onSelect}
        className={`${styles.card} ${isCurrent ? styles.cardSelected : ""}`}
        orientation="horizontal"
      >
        <CardPreview className={styles.horizontalCardImage}>
          <img
            className={styles.horizontalCardImage}
            src={app.image}
            alt={app.name}
          />
        </CardPreview>

        <CardHeader
          header={<Text weight="semibold">{app.name}</Text>}
          description={(
            <Caption1 className={styles.caption}>
              {isDefault ? `${app.description} (default)` : app.description}
            </Caption1>
          )}
        />
      </Card>
    </section>
  );
};
