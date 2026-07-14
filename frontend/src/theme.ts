import { webLightTheme, webDarkTheme, type BrandVariants, type Theme, createLightTheme, createDarkTheme } from '@fluentui/react-components';


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

 const lightTheme: Theme = {
   ...createLightTheme(bronzeTheme), 
};

 const darkTheme: Theme = {
   ...createDarkTheme(bronzeTheme), 
};

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

export const theme = nelotecLightTheme;
