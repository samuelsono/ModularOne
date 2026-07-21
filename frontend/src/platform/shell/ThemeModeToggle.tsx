import { Button, Tooltip, tokens } from '@fluentui/react-components';
import {
  DesktopRegular,
  WeatherMoonRegular,
  WeatherSunnyRegular,
} from '@fluentui/react-icons';
import { useColorMode } from '@platform/shell/ColorModeContext';
import type { ColorMode } from '../../theme';
import { useStyles } from '@platform/shell/navStyles';

const MODE_LABELS: Record<ColorMode, string> = {
  light: 'Light',
  dark: 'Dark',
  system: 'System',
};

function ModeIcon({ mode }: { mode: ColorMode }) {
  const styles = useStyles();

  if (mode === 'dark') {
    return <WeatherMoonRegular className={styles.icon18} />;
  }

  if (mode === 'system') {
    return <DesktopRegular className={styles.icon18} />;
  }

  return <WeatherSunnyRegular className={styles.icon18} />;
}

export function ThemeModeToggle() {
  const { colorMode, cycleColorMode } = useColorMode();
  const label = `Theme: ${MODE_LABELS[colorMode]}. Click to switch.`;

  return (
    <Tooltip content={label} relationship="label">
      <Button
        icon={<ModeIcon mode={colorMode} />}
        appearance="transparent"
        aria-label={label}
        onClick={cycleColorMode}
        style={{ color: tokens.colorBrandBackgroundInverted }}
      />
    </Tooltip>
  );
}
