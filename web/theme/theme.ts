'use client';

import type {} from '@mui/material/themeCssVarsAugmentation';
import { createTheme } from '@mui/material/styles';
import {
  color,
  colorDark,
  colorLight,
  hiphr,
  radius,
  layout,
  buttonSize,
  nav,
  elevation,
  breakpoint,
  typeScale,
  motion,
} from './tokens';

declare module '@mui/material/styles' {
  interface Theme {
    hip: {
      surface: typeof colorLight.surface;
      border: typeof colorLight.border;
      textToken: typeof colorLight.text;
      semantic: typeof colorLight.semantic;
      accent: typeof colorLight.accent;
      layout: typeof layout;
      nav: typeof nav;
      elevation: typeof elevation;
      motion: typeof motion;
      colorDark: typeof colorDark;
    };
  }
  interface ThemeOptions {
    hip?: Theme['hip'];
  }
}

/** Prompt — same files as HIPHR (app/globals.css @font-face) */
const FONT = '"Prompt", "Segoe UI", "Leelawadee UI", Tahoma, sans-serif';

/** Look of HIPHR.Api/wwwroot (D9): gradient orange buttons, white cards on cream, compact controls. */
export const theme = createTheme({
  cssVariables: true,

  breakpoints: {
    values: {
      xs: breakpoint.mobile,
      sm: breakpoint.mobile,
      md: breakpoint.tablet,
      lg: breakpoint.laptop,
      xl: breakpoint.desktop,
    },
  },

  shape: { borderRadius: radius.md },
  spacing: 4,

  palette: {
    mode: 'light',
    primary: {
      main: hiphr.orange600,
      dark: '#D45700',
      light: hiphr.orange100,
      contrastText: color.text.onBrand,
    },
    error:   { main: color.semantic.error.base,   dark: color.semantic.error.text,   light: color.semantic.error.bg },
    warning: { main: color.semantic.warning.base, dark: color.semantic.warning.text, light: color.semantic.warning.bg },
    info:    { main: color.semantic.info.base,    dark: color.semantic.info.text,    light: color.semantic.info.bg },
    success: { main: color.semantic.success.base, dark: color.semantic.success.text, light: color.semantic.success.bg },
    text: { primary: color.text.primary, secondary: color.text.secondary, disabled: color.text.disabled },
    background: { default: color.surface.page, paper: color.surface.card },
    divider: color.border.subtle,
  },

  typography: {
    fontFamily: FONT,
    // ห้ามลด lineHeight — ภาษาไทยมีสระบนและวรรณยุกต์ซ้อน 2 ชั้น
    h1: { ...typeScale.display, fontFamily: FONT },
    h2: { ...typeScale.pageTitle, fontFamily: FONT },
    h3: { ...typeScale.sectionTitle, fontFamily: FONT },
    body1: { ...typeScale.body, fontFamily: FONT },
    body2: { ...typeScale.small, fontFamily: FONT },
    subtitle1: { ...typeScale.bodyStrong, fontFamily: FONT },
    subtitle2: { ...typeScale.label, fontFamily: FONT },
    caption: { ...typeScale.caption, fontFamily: FONT },
    button: { ...typeScale.button, fontFamily: FONT, textTransform: 'none' },
  },

  hip: {
    surface: color.surface,
    border: color.border,
    textToken: color.text,
    semantic: color.semantic,
    accent: color.accent,
    layout,
    nav,
    elevation,
    motion,
    colorDark,
  },

  components: {
    MuiCssBaseline: {
      styleOverrides: {
        'html, body': { height: '100%' },
        body: {
          backgroundColor: 'var(--color-surface-page)',
          color: 'var(--color-text-primary)',
          ...typeScale.body,
        },
        'td, th, .tabular': { fontVariantNumeric: 'tabular-nums' },
        ':focus-visible': {
          outline: `var(--focus-ring-width, 2px) solid var(--color-focus-ring)`,
          outlineOffset: 2,
        },
      },
    },

    MuiButton: {
      defaultProps: { variant: 'contained', disableElevation: true },
      styleOverrides: {
        root: {
          borderRadius: radius.md,
          minHeight: buttonSize.md,
          paddingInline: 18,
          transition: `transform 80ms, filter ${motion.fast}ms, background-color ${motion.fast}ms`,
          '&:active': { transform: 'translateY(1px)' },
        },
        sizeLarge: { minHeight: buttonSize.lg, paddingInline: 24, fontSize: 15 },
        sizeMedium: { minHeight: buttonSize.md, paddingInline: 18 },
        sizeSmall: { minHeight: buttonSize.sm, paddingInline: 12, borderRadius: 9, ...typeScale.small, fontWeight: 600 },
        contained: {
          background: hiphr.buttonGradient,
          color: color.text.onBrand,
          boxShadow: hiphr.buttonShadow,
          '&:hover': { background: hiphr.buttonGradient, filter: 'brightness(1.05)', boxShadow: hiphr.buttonShadow },
          '&.Mui-disabled': { background: color.surface.sunken, color: color.text.disabled, boxShadow: 'none' },
          '&.MuiButton-containedError': {
            background: 'linear-gradient(135deg, #F0646A, #E5484D)',
            boxShadow: '0 4px 12px rgba(229, 72, 77, .25)',
          },
        },
        outlined: {
          backgroundColor: color.surface.card,
          border: 'none',
          boxShadow: `inset 0 0 0 1.5px ${hiphr.orange200}`,
          color: hiphr.orange600,
          '&:hover': { backgroundColor: hiphr.orange50, border: 'none' },
          '&.Mui-disabled': { border: 'none', boxShadow: `inset 0 0 0 1.5px ${color.border.subtle}`, color: color.text.disabled },
          '&.MuiButton-outlinedError': {
            color: color.semantic.error.text,
            boxShadow: `inset 0 0 0 1.5px ${color.semantic.error.border}`,
            '&:hover': { backgroundColor: color.semantic.error.bg },
          },
        },
        text: {
          color: hiphr.orange600,
          '&:hover': { backgroundColor: hiphr.orange50 },
        },
      },
    },

    MuiIconButton: {
      styleOverrides: {
        root: {
          width: layout.minTouch,
          height: layout.minTouch,
          borderRadius: radius.pill,
          color: color.text.secondary,
          '&:hover': { backgroundColor: hiphr.orange50, color: hiphr.orange600 },
        },
        sizeSmall: { width: 32, height: 32 },
      },
    },

    MuiCard: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          backgroundColor: color.surface.card,
          borderRadius: radius.card,
          boxShadow: hiphr.cardShadow,
        },
      },
    },
    MuiPaper: { styleOverrides: { rounded: { borderRadius: radius.card } } },

    MuiDialog: {
      styleOverrides: {
        paper: { borderRadius: 22, boxShadow: '0 30px 80px rgba(43, 26, 14, .28), 0 8px 24px rgba(240, 100, 0, .12)' },
      },
    },
    MuiDialogTitle: { styleOverrides: { root: { ...typeScale.sectionTitle, paddingTop: 20 } } },
    MuiBackdrop: { styleOverrides: { root: { '&:not(.MuiBackdrop-invisible)': { backgroundColor: 'rgba(43, 26, 14, .38)' } } } },

    MuiTextField: {
      defaultProps: { variant: 'outlined', slotProps: { inputLabel: { shrink: true } } },
    },
    // ช่องกรอกแบบ HIPHR: พื้นขาว ขอบ 1.5px สีครีมเข้ม โฟกัสขอบส้ม + เงา · label อยู่เหนือช่อง
    MuiOutlinedInput: {
      defaultProps: { notched: false },
      styleOverrides: {
        root: {
          backgroundColor: color.surface.card,
          borderRadius: radius.md,
          minHeight: 42,
          transition: `box-shadow ${motion.fast}ms ease`,
          '& .MuiOutlinedInput-notchedOutline': {
            border: `1.5px solid ${hiphr.line}`,
            '& legend': { display: 'none' },
          },
          '&:hover:not(.Mui-disabled):not(.Mui-focused):not(.Mui-error) .MuiOutlinedInput-notchedOutline': {
            borderColor: hiphr.orange200,
          },
          '&.Mui-focused': {
            boxShadow: color.focusRingGlow,
            '& .MuiOutlinedInput-notchedOutline': { border: `1.5px solid ${hiphr.orange400}` },
          },
          '&.Mui-error .MuiOutlinedInput-notchedOutline': { border: `1.5px solid ${color.semantic.error.base}` },
          '&.Mui-disabled': {
            backgroundColor: color.surface.sunken,
            color: color.text.disabled,
            '& .MuiOutlinedInput-notchedOutline': { borderColor: color.border.subtle },
          },
          '&.MuiInputBase-sizeSmall': { minHeight: 36 },
          '&.MuiInputBase-sizeSmall .MuiOutlinedInput-input': { paddingBlock: 7, paddingInline: 12 },
        },
        input: {
          paddingBlock: 9.5,
          paddingInline: 14,
          ...typeScale.body,
          color: color.text.primary,
          '&::placeholder': { color: color.text.tertiary, opacity: 1 },
        },
        multiline: { padding: 0 },
        notchedOutline: { '& legend': { display: 'none' } },
      },
    },
    MuiSelect: {
      styleOverrides: {
        select: { ...typeScale.body, color: color.text.primary },
        icon: { right: 10, color: color.text.tertiary, fontSize: 20, transition: `transform ${motion.fast}ms ease` },
        iconOpen: { color: hiphr.orange600 },
      },
    },
    MuiInputLabel: {
      defaultProps: { shrink: true },
      styleOverrides: {
        root: {
          position: 'static',
          transform: 'none',
          marginBottom: 6,
          ...typeScale.label,
          color: color.text.secondary,
          '&.Mui-disabled': { color: color.text.disabled },
          '&.Mui-focused': { color: hiphr.orange600 },
          '&.Mui-error': { color: color.semantic.error.text },
        },
        outlined: { position: 'static', transform: 'none', maxWidth: 'none' },
        sizeSmall: { ...typeScale.label, marginBottom: 6 },
      },
    },
    MuiFormLabel: { styleOverrides: { root: { ...typeScale.label, color: color.text.secondary } } },
    MuiFormHelperText: {
      styleOverrides: { root: { marginInline: 0, marginTop: 6, ...typeScale.caption } },
    },
    MuiFormControlLabel: { styleOverrides: { label: { ...typeScale.body } } },
    MuiSwitch: {
      styleOverrides: {
        switchBase: { '&.Mui-checked': { color: hiphr.orange500 }, '&.Mui-checked + .MuiSwitch-track': { backgroundColor: hiphr.orange400 } },
      },
    },
    MuiCheckbox: { styleOverrides: { root: { '&.Mui-checked': { color: hiphr.orange500 } } } },
    MuiRadio: { styleOverrides: { root: { '&.Mui-checked': { color: hiphr.orange500 } } } },

    MuiMenu: {
      styleOverrides: {
        paper: { borderRadius: 14, boxShadow: hiphr.cardShadow, padding: 6 },
        list: { padding: 0 },
      },
    },
    MuiPopover: { styleOverrides: { paper: { borderRadius: 14, boxShadow: hiphr.cardShadow } } },
    // Autocomplete adds its own padding around the input: keep it as tall as the other fields (42)
    MuiAutocomplete: {
      styleOverrides: {
        paper: { borderRadius: 14, boxShadow: hiphr.cardShadow },
        option: { ...typeScale.small },
        inputRoot: { '&.MuiOutlinedInput-root': { paddingBlock: 3, paddingLeft: 6 } },
        input: { '&.MuiOutlinedInput-input': { paddingBlock: 6.5 } },
        tag: { margin: 2 },
      },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: {
          minHeight: 38,
          borderRadius: 9,
          ...typeScale.small,
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          '&:hover': { backgroundColor: hiphr.orange50 },
          '&.Mui-selected': {
            backgroundColor: hiphr.orange100,
            color: hiphr.orange600,
            fontWeight: 600,
            '&:hover': { backgroundColor: hiphr.orange100 },
            '&::after': { content: '"✓"', marginLeft: 'auto', fontSize: 13, fontWeight: 700, color: hiphr.orange600 },
          },
        },
      },
    },

    // เมนูด้านข้าง
    MuiListItemButton: {
      styleOverrides: {
        root: {
          minHeight: nav.mainHeight,
          borderRadius: nav.radius,
          marginBottom: nav.gap,
          color: color.text.secondary,
          '&:hover': { backgroundColor: hiphr.orange50, color: hiphr.orange600 },
          '&.Mui-selected': {
            backgroundColor: hiphr.orange100,
            color: hiphr.orange600,
            '&:hover': { backgroundColor: hiphr.orange100 },
          },
        },
      },
    },

    MuiTabs: {
      styleOverrides: {
        root: { minHeight: 44 },
        indicator: { height: 3, borderRadius: 3, backgroundColor: hiphr.orange600 },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          minHeight: 44,
          paddingInline: 14,
          ...typeScale.bodyStrong,
          color: color.text.secondary,
          '&.Mui-selected': { color: hiphr.orange600 },
        },
      },
    },

    MuiLink: {
      styleOverrides: {
        root: {
          color: hiphr.orange600,
          textUnderlineOffset: 3,
          '&:hover': { textDecoration: 'underline' },
        },
      },
    },

    // ป้ายสถานะแบบ badge ของ HIPHR: เม็ดยา ตัวหนา
    MuiChip: {
      styleOverrides: {
        root: { height: 26, borderRadius: radius.pill, ...typeScale.label },
        label: { paddingInline: 10 },
        sizeSmall: { height: 22, fontSize: 12, '& .MuiChip-label': { paddingInline: 8 } },
      },
    },

    MuiAlert: { styleOverrides: { root: { borderRadius: 14, ...typeScale.small } } },

    // ตารางแบบ HIPHR: หัวตารางตัวเล็กสีน้ำตาลอ่อน เส้นแบ่งแถวจางๆ แถว hover สีครีม
    MuiTable: { styleOverrides: { root: { borderCollapse: 'separate', borderSpacing: 0 } } },
    MuiTableContainer: {
      styleOverrides: {
        root: {
          borderRadius: radius.card,
          overflow: 'hidden',
          backgroundColor: color.surface.card,
          boxShadow: hiphr.cardShadow,
          padding: 16,
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: { ...typeScale.small, paddingBlock: 10, paddingInline: 12, borderBottom: `1px solid ${color.border.subtle}` },
        head: {
          ...typeScale.label,
          color: color.text.secondary,
          backgroundColor: color.surface.card,
          borderBottom: `1px solid ${hiphr.line}`,
          whiteSpace: 'nowrap',
        },
      },
    },
    MuiTableRow: {
      styleOverrides: {
        root: {
          '&.MuiTableRow-hover:hover > td': { backgroundColor: hiphr.orange50 },
          '&:last-of-type > td': { borderBottom: 'none' },
        },
      },
    },
    MuiPagination: { defaultProps: { color: 'primary', shape: 'rounded' } },
    MuiTooltip: { styleOverrides: { tooltip: { borderRadius: 8, ...typeScale.caption, backgroundColor: hiphr.ink } } },
    MuiDivider: { styleOverrides: { root: { borderColor: color.border.subtle } } },
  },
});

export default theme;
