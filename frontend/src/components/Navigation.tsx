import { useState } from "react";
import { Button, Divider, makeStyles, MenuDivider, Persona, SearchBox, Subtitle2, webLightTheme } from "@fluentui/react-components";
import { SettingsRegular, ChatHelpRegular, QuestionCircleRegular, SignOutRegular, ThumbLikeRegular, BugRegular } from "@fluentui/react-icons";
import {
  Menu,
  MenuTrigger,
  MenuList,
  MenuItem,
  MenuPopover,
} from "@fluentui/react-components";
import { useNavigate } from "react-router-dom";
import { usePageSearch } from "../context/PageSearchContext";
import { NotificationsDialog } from "./NotificationsDialog";
import { HelpCenterDrawer } from "./HelpCenterDrawer";
import { SupportDialog } from "./SupportDialog";
import { AppLauncher } from "./AppLauncher";
import { useAuth } from "../context/AuthContext";
import { useHelpDrawer } from "../context/HelpDrawerContext";
import type { TicketType } from "../types/support";
import { theme as localTheme  } from "../theme";

export const theme = localTheme;

export const useStyles = makeStyles({
  sideNav: {
    display: 'flex',
    flex: 1,
    flexDirection: 'column',
    alignItems: 'center',
    paddingTop: '10px',
    width: '68px',
    height: '94vh',
   },
   colLink: {
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      textDecoration: 'none',
    },
    nav: {
      padding: '10px',
      justifyContent: 'space-between',
      alignItems: 'center',
      display: 'flex',
      backgroundColor: theme.colorNeutralBackground5,
    },
    activeLink: {
        color: theme.colorNeutralForeground2BrandHover,
        borderBlockColor: theme.colorBrandForegroundInverted,
        fontWeight: 'bold',
    },
    ul: {
      listStyleType: 'none',
      margin: 0,
      padding: 0,
      display: 'flex',
      gap: '10px',
    },
    navGroup: {
      margin: 0,
      padding: 0,
      display: 'flex',
      alignItems: 'center',
      gap: '10px',
    },
    li: {
      display: 'inline',
    },
    a: {
      textDecoration: 'none',
      color: '#007bff',
    },
    searchBox: {
      width: '400px',
    },
    icon18: { fontSize: "18px" },
    icon24: { fontSize: "24px" },
    icon32: { fontSize: "32px" },
    icon48: { fontSize: "48px" },
});

const Navigation = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const { query, setQuery, placeholder, enabled } = usePageSearch();

  return (
    <nav className={`${styles.nav} fixed top-0 left-0 right-0 border-b`} style={{ backgroundColor: theme.colorBrandBackground, color: theme.colorBrandBackgroundInverted }}>
        <div className={styles.navGroup}>
          <AppLauncher>
            <div/>
          </AppLauncher>

         <Subtitle2>Chronos</Subtitle2>
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
          <Button
            icon={<SettingsRegular className={styles.icon18} />}
            appearance="transparent"
            aria-label="Settings"
            onClick={() => navigate('/settings')}
            style={{ color: theme.colorBrandBackgroundInverted }}
          />
          <HelpCenterDrawer />
          <Divider vertical style={{ height: "100%", marginInline: "5px" }} />
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
  const displayName = user?.displayName || user?.username || "User";
  const [supportMode, setSupportMode] = useState<TicketType | null>(null);

  async function handleLogout() {
    await logout();
    navigate("/auth/login", { replace: true });
  }

  return (
    <>
      <Menu positioning={{ autoSize: true }}>
        <MenuTrigger disableButtonEnhancement>
          <Persona avatar={{ name: displayName, color: "colorful" }} presence={{ status: "available" }} />
        </MenuTrigger>

        <MenuPopover>
          <MenuList className="w-[200px]">
            <MenuItem>{displayName}</MenuItem>
            <MenuItem disabled>{user?.email}</MenuItem>
            <MenuItem disabled>Toggle Availability</MenuItem>
            <MenuItem disabled>Team Management</MenuItem>
            <MenuDivider />
            <MenuItem icon={<SettingsRegular />} onClick={() => navigate('/settings')}>Settings</MenuItem>
            <MenuItem icon={<QuestionCircleRegular />} onClick={() => setHelpOpen(true)}>Help</MenuItem>
            <MenuItem icon={<ChatHelpRegular />} onClick={() => setSupportMode('Ticket')}>Support</MenuItem>
            <MenuDivider />
            <MenuItem>Privacy</MenuItem>
            <MenuItem>Terms of Service</MenuItem>
            <MenuDivider />
            <MenuItem icon={<ThumbLikeRegular />} onClick={() => setSupportMode('Feedback')}>Feedback</MenuItem>
            <MenuItem icon={<BugRegular />} onClick={() => setSupportMode('BugReport')}>Report a Bug</MenuItem>
            <MenuDivider />
            <MenuItem icon={<SignOutRegular />} onClick={() => void handleLogout()}>Logout</MenuItem>
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
