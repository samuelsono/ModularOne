import { makeStyles, tokens } from '@fluentui/react-components';

export const useStyles = makeStyles({
  sideNav: {
    display: 'flex',
    flex: 1,
    flexDirection: 'column',
    alignItems: 'center',
    paddingTop: '10px',
    width: '78px',
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
  },
  navLink: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    width: '78px',
    height: '56px',
    textDecoration: 'none',
    color: tokens.colorNeutralForeground2,
    borderLeft: `2px solid transparent`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      color: tokens.colorNeutralForeground1,
    },
  },
  activeLink: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    color: tokens.colorBrandForeground1,
    borderLeftColor: tokens.colorBrandStroke1,
    fontWeight: 600,
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
    color: tokens.colorBrandForeground1,
  },
  searchBox: {
    width: '400px',
  },
  icon18: { fontSize: '18px' },
  icon24: { fontSize: '24px' },
  icon32: { fontSize: '32px' },
  icon48: { fontSize: '48px' },
});
