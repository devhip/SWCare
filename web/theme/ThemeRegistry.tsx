'use client';

import { ThemeProvider } from '@mui/material/styles';
import theme from './theme';

/**
 * ครอบเฉพาะ ThemeProvider — AppRouterCacheProvider กับ CssBaseline
 * อยู่ใน app/layout.tsx แล้ว ไม่ต้องซ้ำ
 */
export default function ThemeRegistry({ children }: { children: React.ReactNode }) {
  return <ThemeProvider theme={theme}>{children}</ThemeProvider>;
}
