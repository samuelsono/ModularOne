import { useState } from 'react';
import {
  Button,
  Divider,
  Menu,
  MenuDivider,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Persona,
  SearchBox,
  Subtitle2,
  tokens,
} from '@fluentui/react-components';
import {
  BugRegular,
  ChatHelpRegular,
  QuestionCircleRegular,
  SettingsRegular,
  SignOutRegular,
  ThumbLikeRegular,
} from '@fluentui/react-icons';
import { useNavigate } from 'react-router-dom';
import { usePageSearch } from '@platform/shell/PageSearchContext';
import { AppLauncher } from '@platform/shell/AppLauncher';
import { useAuth } from '@platform/auth/AuthContext';
import { useStyles } from '@platform/shell/navStyles';
import { NotificationsDialog } from '@modules/notifications/components/NotificationsDialog';
import { HelpCenterDrawer } from '@modules/help/components/HelpCenterDrawer';
import { SupportDialog } from '@modules/support/components/SupportDialog';
import { useHelpDrawer } from '@modules/help/context/HelpDrawerContext';
import type { TicketType } from '@modules/support/types/support';
import { useActiveApp } from '@platform/shell/ActiveAppContext';
import { ThemeModeToggle } from '@platform/shell/ThemeModeToggle';

/** App-shell composition: may import feature modules; platform shell must not. */
const Navigation = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const { query, setQuery, placeholder, enabled } = usePageSearch();
  const appname = useActiveApp();

  return (
    <nav
      className={`${styles.nav} fixed top-0 left-0 right-0 border-b`}
      style={{
        backgroundColor: tokens.colorBrandBackground,
        color: tokens.colorBrandBackgroundInverted,
        borderBottomColor: tokens.colorNeutralStrokeOnBrand,
      }}
    >
      <div className={styles.navGroup}>
        <AppLauncher>
          <div />
        </AppLauncher>
        <Subtitle2>Chronos {appname.currentModule?.name ? ` - ${appname.currentModule?.name}` : ''}</Subtitle2>
      </div>

      <div className={styles.navGroup}>
        <SearchBox
          placeholder={placeholder}
          className={styles.searchBox}
          value={query}
          onChange={(_, data) => setQuery(data.value)}
          disabled={!enabled}
        />
      </div>

      <div className={styles.navGroup}>
        <NotificationsDialog />
        <ThemeModeToggle />
        <Button
          icon={<SettingsRegular className={styles.icon18} />}
          appearance="transparent"
          aria-label="Settings"
          onClick={() => navigate('/settings')}
          style={{ color: tokens.colorBrandBackgroundInverted }}
        />
        <HelpCenterDrawer />
        <Divider vertical style={{ height: '100%', marginInline: '5px' }} />
        <AvatarMenu />
      </div>
    </nav>
  );
};

export default Navigation;

export const AvatarMenu = () => {
  const { user, logout } = useAuth();
  const { setOpen: setHelpOpen } = useHelpDrawer();
  const navigate = useNavigate();
  const displayName = user?.displayName || user?.username || 'User';
  const [supportMode, setSupportMode] = useState<TicketType | null>(null);

  async function handleLogout() {
    await logout();
    navigate('/auth/login', { replace: true });
  }

  return (
    <>
      <Menu positioning={{ autoSize: true }}>
        <MenuTrigger disableButtonEnhancement>
          <Persona avatar={{ name: displayName, color: 'colorful' }} presence={{ status: 'available' }} />
        </MenuTrigger>

        <MenuPopover>
          <MenuList className="w-[200px]">
            <MenuItem>{displayName}</MenuItem>
            <MenuItem disabled>{user?.email}</MenuItem>
            <MenuItem disabled>Toggle Availability</MenuItem>
            <MenuItem disabled>Team Management</MenuItem>
            <MenuDivider />
            <MenuItem icon={<SettingsRegular />} onClick={() => navigate('/settings')}>
              Settings
            </MenuItem>
            <MenuItem icon={<QuestionCircleRegular />} onClick={() => setHelpOpen(true)}>
              Help
            </MenuItem>
            <MenuItem icon={<ChatHelpRegular />} onClick={() => setSupportMode('Ticket')}>
              Support
            </MenuItem>
            <MenuDivider />
            <MenuItem>Privacy</MenuItem>
            <MenuItem>Terms of Service</MenuItem>
            <MenuDivider />
            <MenuItem icon={<ThumbLikeRegular />} onClick={() => setSupportMode('Feedback')}>
              Feedback
            </MenuItem>
            <MenuItem icon={<BugRegular />} onClick={() => setSupportMode('BugReport')}>
              Report a Bug
            </MenuItem>
            <MenuDivider />
            <MenuItem icon={<SignOutRegular />} onClick={() => void handleLogout()}>
              Logout
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>

      <SupportDialog
        open={supportMode !== null}
        onClose={() => setSupportMode(null)}
        initialMode={supportMode ?? 'Ticket'}
      />
    </>
  );
};
