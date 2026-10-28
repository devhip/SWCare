"use client";

import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { formatDateTime } from "@/lib/format";
import type { Activity } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";

type Change = { Field: string; Old?: string | null; New?: string | null };

function changedFields(data: string | null): string[] {
  if (!data) return [];
  try {
    return ((JSON.parse(data) as { Changes?: Change[] }).Changes ?? []).map((change) => change.Field);
  } catch {
    return [];
  }
}

/** Every status change and edit, oldest first (plan 4.3). */
export default function Timeline({ activities }: { activities: Activity[] }) {
  const { t, i18n } = useTranslation("ticket");

  return (
    <Stack spacing={0}>
      {activities.map((activity, index) => {
        const fields = changedFields(activity.Data);
        return (
          <Stack key={activity.Id} direction="row" spacing={1.5}>
            <Stack sx={{ alignItems: "center", pt: 0.75 }}>
              <Box sx={{ width: 10, height: 10, borderRadius: "50%", bgcolor: color.accent.brand, flexShrink: 0 }} />
              {index < activities.length - 1 ? <Box sx={{ width: 2, flex: 1, bgcolor: color.border.subtle, my: 0.5 }} /> : null}
            </Stack>
            <Stack sx={{ pb: 2, minWidth: 0 }}>
              <Typography sx={{ ...typeScale.bodyStrong16, color: color.text.primary }}>{t(`activity.${activity.Type}`)}</Typography>
              {fields.length ? (
                <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>
                  {t("activity.changed", { fields: fields.map((field) => t(`changeField.${field}`)).join(", ") })}
                </Typography>
              ) : null}
              {activity.Note ? (
                <Typography sx={{ ...typeScale.bodySm14, color: color.text.primary, whiteSpace: "pre-wrap" }}>{activity.Note}</Typography>
              ) : null}
              <Typography sx={{ ...typeScale.caption12, color: color.text.secondary }}>
                {activity.Actor?.DisplayName ?? t("activity.system")} · {formatDateTime(activity.CreatedAt, i18n.language)}
              </Typography>
            </Stack>
          </Stack>
        );
      })}
    </Stack>
  );
}
