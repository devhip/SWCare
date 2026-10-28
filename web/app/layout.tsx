import type { Metadata } from "next";
import { Noto_Sans_Thai } from "next/font/google";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import CssBaseline from "@mui/material/CssBaseline";
import AppProviders from "./components/AppProviders";
import "./globals.css";

const notoSansThai = Noto_Sans_Thai({
  weight: ["400", "500", "600", "700"],
  subsets: ["thai", "latin"],
  display: "swap",
  variable: "--font-noto-sans-thai",
});

export const metadata: Metadata = {
  title: "SWCare",
  description: "ระบบแจ้งงานโปรแกรมเมอร์",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="th" data-theme="light" className={`${notoSansThai.variable} ${notoSansThai.className}`}>
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
