"use client";

import { useTranslation } from "react-i18next";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AppShell from "@/app/components/AppShell";
import { color, radiusPx, typeScale } from "@/theme/tokens";

/** งานของฉัน — filled in on day 3 of the plan (13.1). */
export default function MyTasksPage() {
  const { t } = useTranslation("navigation");
  const { t: tc } = useTranslation("common");

  return (
    <AppShell>
      <Stack spacing={3}>
        <Typography component="h1" sx={{ ...typeScale.h2_24, color: color.text.primary }}>
          {t("myTasks")}
        </Typography>
        <Stack
          spacing={1}
          sx={{ p: 3, bgcolor: color.surface.card, border: `1px solid ${color.border.subtle}`, borderRadius: radiusPx.md }}
        >
          <Typography sx={{ ...typeScale.bodyStrong16, color: color.text.primary }}>{tc("comingSoonTitle")}</Typography>
          <Typography sx={{ ...typeScale.body16, color: color.text.secondary }}>{tc("comingSoonDesc")}</Typography>
        </Stack>
      </Stack>
    </AppShell>
  );
}
