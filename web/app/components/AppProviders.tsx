"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { ToastContainer } from "react-toastify";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import ThemeRegistry from "@/theme/ThemeRegistry";
import { useAuthStore } from "@/stores/auth.store";
import { applyStoredLang, useLangStore } from "@/stores/lang.store";
import { loadRuntimeConfig } from "@/lib/runtimeConfig";
import { color, typeScale } from "@/theme/tokens";
import "@/lib/i18n";
import "react-toastify/dist/ReactToastify.css";

export default function AppProviders({ children }: { children: React.ReactNode }) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } },
      })
  );
  const [booted, setBooted] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      await loadRuntimeConfig();
      await useAuthStore.persist.rehydrate();
      await useLangStore.persist.rehydrate();
      useAuthStore.setState({ hasHydrated: true });
      applyStoredLang();
      if (!cancelled) setBooted(true);
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <QueryClientProvider client={client}>
      <ThemeRegistry>
        {booted ? (
          children
        ) : (
          <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", bgcolor: color.surface.page }}>
            <Typography sx={{ ...typeScale.body, color: color.text.secondary }}>กำลังโหลด…</Typography>
          </Box>
        )}
        <ToastContainer position="top-right" autoClose={5000} newestOnTop closeOnClick pauseOnHover limit={3} theme="light" />
      </ThemeRegistry>
    </QueryClientProvider>
  );
}
