import type { Metadata } from "next";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import CssBaseline from "@mui/material/CssBaseline";
import AppProviders from "./components/AppProviders";
import "./globals.css";

// Font: Prompt from public/fonts, declared in globals.css (same files as HIPHR, D9).

export const metadata: Metadata = {
  title: "SWCare",
  description: "ระบบแจ้งงานโปรแกรมเมอร์",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="th" data-theme="light">
      <body>
        <AppRouterCacheProvider>
          <AppProviders>
            <CssBaseline />
            {children}
          </AppProviders>
        </AppRouterCacheProvider>
      </body>
    </html>
  );
}
