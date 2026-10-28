"use client";

import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { card, color, typeScale } from "@/theme/tokens";
import LanguageSwitch from "./LanguageSwitch";

/** Centered card for the login and forced password-change pages. */
export default function AuthCard({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  const { t } = useTranslation("common");

  return (
    <Box component="main" sx={{ minHeight: "100vh", bgcolor: color.surface.page, display: "flex", flexDirection: "column", px: 2, py: 3 }}>
      <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
        <LanguageSwitch />
      </Box>
      <Box sx={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", py: 4 }}>
        <Stack
          spacing={3}
          sx={{
            width: "100%",
            maxWidth: 440,
            ...card,
            px: { xs: 3, md: 5 },
            py: { xs: 4, md: 5 },
          }}
        >
          <Stack spacing={0.5}>
            <Typography sx={{ ...typeScale.sectionTitle, color: color.text.brand }}>{t("appName")}</Typography>
            <Typography component="h1" sx={{ ...typeScale.display, color: color.text.primary }}>
              {title}
            </Typography>
            {subtitle ? <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{subtitle}</Typography> : null}
          </Stack>
          {children}
        </Stack>
      </Box>
    </Box>
  );
}
