import { makeStyles } from '@fluentui/react-components';
import { theme as appTheme } from '../../theme';

export const theme = appTheme;

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
  icon18: { fontSize: '18px' },
  icon24: { fontSize: '24px' },
  icon32: { fontSize: '32px' },
  icon48: { fontSize: '48px' },
});
