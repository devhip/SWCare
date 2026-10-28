'use client';

import type {} from '@mui/material/themeCssVarsAugmentation';
import { createTheme } from '@mui/material/styles';
import {
  color,
  colorDark,
  colorLight,
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

const FONT = 'var(--font-noto-sans-thai), -apple-system, "Segoe UI", sans-serif';

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
      main: color.brand[500],
      dark: color.brand[600],
      light: color.brand[50],
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
    h1: { ...typeScale.h1_28, fontFamily: FONT },
    h2: { ...typeScale.h2_24, fontFamily: FONT },
    h3: { ...typeScale.h3_20, fontFamily: FONT },
    body1: { ...typeScale.body16, fontFamily: FONT },
    body2: { ...typeScale.bodySm14, fontFamily: FONT },
    subtitle1: { ...typeScale.bodyStrong16, fontFamily: FONT },
    caption: { ...typeScale.caption12, fontFamily: FONT },
    button: { ...typeScale.button16, fontFamily: FONT, textTransform: 'none' },
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
        root: { borderRadius: radius.lg, minHeight: buttonSize.lg, paddingInline: 24 },
        sizeLarge: { minHeight: buttonSize.lg, paddingInline: 28 },
        sizeMedium: { minHeight: buttonSize.lg, paddingInline: 24 },
        // sm สูง 40 แต่พื้นที่กดต้อง >= 48 → ขยายด้วย pseudo element
        sizeSmall: {
          minHeight: buttonSize.sm,
          paddingInline: 16,
          fontSize: typeScale.bodySm14.fontSize,
          position: 'relative',
          '&::after': {
            content: '""',
            position: 'absolute',
            insetInline: 0,
            top: '50%',
            transform: 'translateY(-50%)',
            height: layout.minTouch,
          },
        },
        contained: {
          backgroundColor: color.brand[500],
          color: color.text.onBrand,
          '&:hover': { backgroundColor: color.brand[600] },
          '&:active': { backgroundColor: color.brand[800] },
          '&.Mui-disabled': { backgroundColor: color.surface.sunken, color: color.text.disabled },
        },
        outlined: {
          backgroundColor: color.surface.card,
          borderColor: color.border.default,
          color: color.text.brand,
          '&:hover': { backgroundColor: color.surface.sunken, borderColor: color.border.strong },
          '&.Mui-disabled': { borderColor: color.border.subtle, color: color.text.disabled },
          '&.MuiButton-outlinedError': {
            color: color.semantic.error.text,
            borderColor: color.semantic.error.border,
            '&:hover': { backgroundColor: color.semantic.error.bg, borderColor: color.semantic.error.base },
          },
        },
        text: {
          color: color.text.secondary,
          '&:hover': { backgroundColor: color.surface.sunken, color: color.text.primary },
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
          '&:hover': { backgroundColor: color.surface.sunken },
        },
      },
    },

    MuiCard: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          backgroundColor: color.surface.card,
          border: `1px solid ${color.border.subtle}`,
          borderRadius: radius.md,
          boxShadow: elevation[1],
        },
      },
    },
    MuiPaper: { styleOverrides: { rounded: { borderRadius: radius.md } } },

    MuiTextField: {
      defaultProps: { variant: 'outlined', slotProps: { inputLabel: { shrink: true } } },
    },
    // Dropdown / Field + Text input — Figma 118:38 · Design System
    // filled · พื้น surface/page ไม่มีขอบ · Focus/Error เท่านั้นที่ขอบสี · label เหนือช่อง
    MuiOutlinedInput: {
      defaultProps: { notched: false },
      styleOverrides: {
        root: {
          backgroundColor: color.surface.page,
          borderRadius: radius.md,
          minHeight: 56,
          transition: `background-color ${motion.fast}ms ease, box-shadow ${motion.fast}ms ease`,
          '& .MuiOutlinedInput-notchedOutline': {
            border: 'none',
            '& legend': { display: 'none' },
          },
          '&:hover:not(.Mui-disabled):not(.Mui-focused):not(.Mui-error)': {
            backgroundColor: color.neutral[200],
          },
          '&.Mui-focused': {
            backgroundColor: color.surface.card,
            boxShadow: color.focusRingGlow,
            '& .MuiOutlinedInput-notchedOutline': {
              border: `2px solid ${color.brand[500]}`,
              '& legend': { display: 'none' },
            },
          },
          '&.Mui-error': {
            backgroundColor: color.surface.card,
            '& .MuiOutlinedInput-notchedOutline': {
              border: `1.5px solid ${color.semantic.error.base}`,
              '& legend': { display: 'none' },
            },
          },
          '&.MuiInputBase-colorSuccess': {
            backgroundColor: color.surface.card,
            '& .MuiOutlinedInput-notchedOutline': { border: `1.5px solid ${color.semantic.success.base}` },
          },
          '&.Mui-disabled': { backgroundColor: color.surface.page, color: color.text.disabled },
          // sm 48 — แถบกรอง / header (Figma DropdownField h=48)
          '&.MuiInputBase-sizeSmall': {
            minHeight: 48,
          },
          '&.MuiInputBase-sizeSmall .MuiOutlinedInput-input': {
            paddingBlock: 11,
            paddingInline: 20,
          },
        },
        input: {
          paddingInline: 20,
          ...typeScale.body16,
          color: color.text.primary,
          '&::placeholder': { color: color.text.tertiary, opacity: 1 },
        },
        notchedOutline: {
          '& legend': { display: 'none' },
        },
      },
    },
    MuiSelect: {
      styleOverrides: {
        select: {
          ...typeScale.body16,
          color: color.text.primary,
        },
        icon: {
          right: 14,
          color: color.text.tertiary,
          fontSize: 20,
          transition: `transform ${motion.fast}ms ease`,
        },
        iconOpen: {
          color: color.brand[500],
        },
      },
    },
    MuiInputLabel: {
      defaultProps: { shrink: true },
      styleOverrides: {
        root: {
          position: 'static',
          transform: 'none',
          marginBottom: 8,
          ...typeScale.bodyStrong16,
          color: color.text.primary,
          '&.Mui-disabled': { color: color.text.disabled },
          '&.Mui-focused': { color: color.text.primary },
          '&.Mui-error': { color: color.text.primary },
        },
        // ห้ามตัดขอบ outlined (floating notch)
        outlined: {
          position: 'static',
          transform: 'none',
          maxWidth: 'none',
        },
        sizeSmall: {
          ...typeScale.bodyStrong16,
          marginBottom: 8,
        },
      },
    },
    MuiFormHelperText: {
      styleOverrides: { root: { marginInline: 0, marginTop: 8, ...typeScale.caption12 } },
    },

    MuiMenu: {
      styleOverrides: {
        paper: {
          borderRadius: radius.md,
          border: `1px solid ${color.border.subtle}`,
          boxShadow: elevation[2],
          padding: 8,
        },
        list: { padding: 0 },
      },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: {
          minHeight: 44,
          borderRadius: radius.sm,
          ...typeScale.bodySm14,
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          '&:hover': { backgroundColor: color.surface.sunken },
          '&.Mui-selected': {
            backgroundColor: color.surface.brandSubtle,
            color: color.text.brand,
            fontWeight: 600,
            '&:hover': { backgroundColor: color.surface.brandSubtle },
            // ✓ ขวา — Design System dropdown menu
            '&::after': {
              content: '"✓"',
              marginLeft: 'auto',
              fontSize: 14,
              fontWeight: 700,
              color: color.brand[700],
            },
          },
        },
      },
    },

    // เมนู sidebar: main 48 / sub 44 / radius 12 / gap 4
    MuiListItemButton: {
      styleOverrides: {
        root: {
          minHeight: nav.mainHeight,
          borderRadius: nav.radius,
          marginBottom: nav.gap,
          color: color.text.secondary,
          '&:hover': { backgroundColor: color.surface.sunken },
          '&.Mui-selected': {
            backgroundColor: color.surface.brandSubtle,
            color: color.text.brand,
            fontWeight: 600,
            '&:hover': { backgroundColor: color.surface.brandSubtle },
          },
        },
      },
    },

    MuiLink: {
      styleOverrides: {
        root: {
          color: color.text.brand,
          textUnderlineOffset: 3,
          '&:hover': { color: color.brand[800], textDecoration: 'underline' },
        },
      },
    },

    MuiChip: { styleOverrides: { root: { height: 32, borderRadius: radius.pill, ...typeScale.caption12 } } },

    // ตารางตาม Figma รายการ (ผู้ใช้ระบบ): การ์ด radius/lg 16 · หัวตารางแถบ sunken radius/sm 8
    MuiTable: {
      styleOverrides: {
        root: {
          borderCollapse: 'separate',
          borderSpacing: 0,
        },
      },
    },
    MuiTableContainer: {
      styleOverrides: {
        root: {
          borderRadius: radius.lg,
          overflow: 'hidden',
          backgroundColor: color.surface.card,
          border: `1px solid ${color.border.subtle}`,
          padding: 24,
        },
      },
    },
    MuiTableHead: {
      styleOverrides: {
        root: {
          '& .MuiTableCell-head': {
            ...typeScale.bodySm14,
            backgroundColor: color.surface.sunken,
            borderBottom: 'none',
            color: color.text.primary,
          },
          '& .MuiTableRow-root:first-of-type .MuiTableCell-head:first-of-type': {
            borderTopLeftRadius: radius.sm,
            borderBottomLeftRadius: radius.sm,
          },
          '& .MuiTableRow-root:first-of-type .MuiTableCell-head:last-of-type': {
            borderTopRightRadius: radius.sm,
            borderBottomRightRadius: radius.sm,
          },
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: { ...typeScale.bodySm14, borderBottomColor: color.border.subtle },
        head: { ...typeScale.bodySm14, color: color.text.primary },
      },
    },
    MuiTableRow: { styleOverrides: { root: { height: 50 } } },
    // Table footer — rows-per-page เป็น Select แบบ standard (ไม่ใช่ OutlinedInput ของฟอร์ม)
    MuiTablePagination: {
      styleOverrides: {
        selectRoot: {
          marginLeft: 8,
          marginRight: 16,
          minWidth: 80,
          '&::before, &::after': { display: 'none' },
        },
        select: {
          ...typeScale.bodySm14,
          color: color.text.primary,
          backgroundColor: color.surface.page,
          borderRadius: radius.md,
          border: `1px solid ${color.border.subtle}`,
          paddingTop: 10,
          paddingBottom: 10,
          paddingLeft: 16,
          // ช่องว่างระหว่างตัวเลขกับลูกศร (ไอคอน absolute ใน selectRoot)
          paddingRight: '52px !important',
          minWidth: 56,
          boxSizing: 'border-box',
          textAlign: 'left',
          '&:focus': {
            backgroundColor: color.surface.card,
            borderColor: color.brand[500],
            borderRadius: radius.md,
          },
        },
        selectIcon: {
          color: color.text.tertiary,
          right: 16,
          top: 'calc(50% - 12px)',
          fontSize: 20,
          pointerEvents: 'none',
        },
      },
    },
    MuiTooltip: { styleOverrides: { tooltip: { borderRadius: radius.sm, ...typeScale.caption12 } } },
  },
});

export default theme;
