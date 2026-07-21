import { type BrandVariants, type Theme, createLightTheme, createDarkTheme } from '@fluentui/react-components';


const nelotecGreenTheme: BrandVariants = { 
  10: "#030402",
  20: "#181B10",
  30: "#262C18",
  40: "#30391E",
  50: "#3A4623",
  60: "#455428",
  70: "#50622E",
  80: "#5B7033",
  90: "#667F39",
  100: "#728E3E",
  110: "#7E9D44",
  120: "#8AAD4A",
  130: "#96BC4F",
  140: "#A8CB68",
  150: "#BED78C",
  160: "#D3E4B0"
};

 const nelotecLightTheme: Theme = {
   ...createLightTheme(nelotecGreenTheme), 
};

 const nelotecDarkTheme: Theme = {
   ...createDarkTheme(nelotecGreenTheme), 
};


nelotecDarkTheme.colorBrandForeground1 = nelotecGreenTheme[110];
nelotecDarkTheme.colorBrandForeground2 = nelotecGreenTheme[120];

const bronzeTheme: BrandVariants = { 
  10: "#040301",
  20: "#1D1908",
  30: "#2F2A0D",
  40: "#3C360E",
  50: "#4A420E",
  60: "#584F0C",
  70: "#665C09",
  80: "#756905",
  90: "#84770A",
  100: "#92852D",
  110: "#A09347",
  120: "#AEA160",
  130: "#BCB079",
  140: "#C9BF92",
  150: "#D7CEAC",
  160: "#E4DDC6"
};

 const _lightTheme: Theme = {
   ...createLightTheme(bronzeTheme), 
};

 const darkTheme: Theme = {
   ...createDarkTheme(bronzeTheme), 
};


const talisFleet: BrandVariants = { 
  10: "#030303",
  20: "#181717",
  30: "#272625",
  40: "#343230",
  50: "#403E3B",
  60: "#4D4A47",
  70: "#5B5753",
  80: "#686460",
  90: "#76726D",
  100: "#84807B",
  110: "#928E8A",
  120: "#A09C98",
  130: "#AEABA8",
  140: "#BDBAB7",
  150: "#CBC9C7",
  160: "#DAD8D7"
};

 const talisFleetLight: Theme = {
   ...createLightTheme(talisFleet), 
};

 const talisFleetDark: Theme = {
   ...createDarkTheme(talisFleet), 
};


 talisFleetDark.colorBrandForeground1 = talisFleet[110];
 talisFleetDark.colorBrandForeground2 = talisFleet[120];

const talisGroupTheme: BrandVariants = { 
  10: "#030303",
  20: "#171717",
  30: "#252525",
  40: "#313131",
  50: "#3D3D3D",
  60: "#494949",
  70: "#565656",
  80: "#636363",
  90: "#717171",
  100: "#7F7F7F",
  110: "#8D8D8D",
  120: "#9B9B9B",
  130: "#AAAAAA",
  140: "#B9B9B9",
  150: "#C8C8C8",
  160: "#D7D7D7"
};

 const talisLightTheme: Theme = {
   ...createLightTheme(talisGroupTheme), 
};

 const talisDarkTheme: Theme = {
   ...createDarkTheme(talisGroupTheme), 
};


 talisDarkTheme.colorBrandForeground1 = talisGroupTheme[110];
 talisDarkTheme.colorBrandForeground2 = talisGroupTheme[120];


 darkTheme.colorBrandForeground1 = bronzeTheme[110];
 darkTheme.colorBrandForeground2 = bronzeTheme[120];

//  lightTheme.colorBrandBackground = bronzeTheme[30];


const blackTheme: BrandVariants = { 
  10: "#030303",
  20: "#171717",
  30: "#252525",
  40: "#313131",
  50: "#3D3D3D",
  60: "#494949",
  70: "#565656",
  80: "#636363",
  90: "#717171",
  100: "#7F7F7F",
  110: "#8D8D8D",
  120: "#9B9B9B",
  130: "#AAAAAA",
  140: "#B9B9B9",
  150: "#C8C8C8",
  160: "#D7D7D7"
};

 const lightBlackTheme: Theme = {
   ...createLightTheme(blackTheme), 
};

 const darkBlackTheme: Theme = {
   ...createDarkTheme(blackTheme), 
};


 darkBlackTheme.colorBrandForeground1 = blackTheme[110];
 darkBlackTheme.colorBrandForeground2 = blackTheme[120];


const teamsTheme: BrandVariants = { 
  10: "#020204",
  20: "#16161E",
  30: "#232333",
  40: "#2E2E45",
  50: "#393958",
  60: "#44456C",
  70: "#505181",
  80: "#5C5E96",
  90: "#6A6BA5",
  100: "#7979AE",
  110: "#8888B7",
  120: "#9796C1",
  130: "#A7A6CA",
  140: "#B6B5D3",
  150: "#C6C4DD",
  160: "#D5D4E6"
};

 const lightTeamsTheme: Theme = {
   ...createLightTheme(teamsTheme), 
};

 const darkTeamsTheme: Theme = {
   ...createDarkTheme(teamsTheme), 
};


 darkTeamsTheme.colorBrandForeground1 = teamsTheme[110];
 darkTeamsTheme.colorBrandForeground2 = teamsTheme[120];

export const theme = talisLightTheme;


export const themeNames = {
  nelotecLightTheme: nelotecLightTheme,
  nelotecDarkTheme: nelotecDarkTheme,
  bronzeLightTheme: _lightTheme,
  bronzeDarkTheme: darkTheme,
  talisFleetLight: talisFleetLight,
  talisFleetDark: talisFleetDark,
  talisLightTheme: talisLightTheme,
  talisDarkTheme: talisDarkTheme,
  lightBlackTheme: lightBlackTheme,
  darkBlackTheme: darkBlackTheme,
  lightTeamsTheme: lightTeamsTheme,
  darkTeamsTheme: darkTeamsTheme
}

export type ThemeName = keyof typeof themeNames;

export type ColorMode = 'light' | 'dark' | 'system';

export const COLOR_MODES: ColorMode[] = ['light', 'dark', 'system'];

export const DEFAULT_THEME_NAME: ThemeName = 'talisLightTheme';
export const DEFAULT_COLOR_MODE: ColorMode = 'system';

export const availableThemeNames = Object.keys(themeNames) as ThemeName[];

/** Light/dark pairs for each brand palette configured in settings. */
const themePairs: Array<{ light: ThemeName; dark: ThemeName }> = [
  { light: 'nelotecLightTheme', dark: 'nelotecDarkTheme' },
  { light: 'bronzeLightTheme', dark: 'bronzeDarkTheme' },
  { light: 'talisFleetLight', dark: 'talisFleetDark' },
  { light: 'talisLightTheme', dark: 'talisDarkTheme' },
  { light: 'lightBlackTheme', dark: 'darkBlackTheme' },
  { light: 'lightTeamsTheme', dark: 'darkTeamsTheme' },
];

export function resolveThemeByName(themeName?: string | null): Theme {
  if (!themeName) {
    return themeNames[DEFAULT_THEME_NAME];
  }

  const selectedTheme = themeNames[themeName as ThemeName];
  return selectedTheme ?? themeNames[DEFAULT_THEME_NAME];
}

export function getThemePair(themeName: ThemeName): { light: ThemeName; dark: ThemeName } {
  const pair = themePairs.find(
    (entry) => entry.light === themeName || entry.dark === themeName,
  );

  return pair ?? {
    light: DEFAULT_THEME_NAME,
    dark: 'talisDarkTheme',
  };
}

export function resolveEffectiveThemeName(
  configuredThemeName: ThemeName,
  colorMode: ColorMode,
  prefersDark: boolean,
): ThemeName {
  const pair = getThemePair(configuredThemeName);
  const useDark = colorMode === 'dark' || (colorMode === 'system' && prefersDark);
  return useDark ? pair.dark : pair.light;
}

export function nextColorMode(current: ColorMode): ColorMode {
  const index = COLOR_MODES.indexOf(current);
  return COLOR_MODES[(index + 1) % COLOR_MODES.length] ?? DEFAULT_COLOR_MODE;
}