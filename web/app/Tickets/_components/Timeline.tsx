"use client";

import type { TFunction } from "i18next";
import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { formatDate, formatDateTime } from "@/lib/format";
import type { Activity, TicketPriority } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";

type Data = {
  Changes?: { Field: string }[];
  ActualPriority?: TicketPriority;
  Assignee?: string;
  PlannedDueDate?: string;
  Old?: string | null;
  New?: string | null;
  OldDueDate?: string | null;
  NewDueDate?: string;
  User?: string;
};

function parse(data: string | null): Data {
  if (!data) return {};
  try {
    return JSON.parse(data) as Data;
  } catch {
    return {};
  }
}

/** One line under the activity name, from the JSON the API stored with it. */
function detail(activity: Activity, data: Data, t: TFunction, lang: string): string | null {
  switch (activity.Type) {
    case "Edited":
    case "ClassificationChanged":
      return data.Changes?.length ? t("activity.changed", { fields: data.Changes.map((c) => t(`changeField.${c.Field}`)).join(", ") }) : null;
    case "Triaged":
      return [
        data.ActualPriority ? t(`priority.${data.ActualPriority}`) : null,
        data.Assignee ? `${t("field.assignee")} ${data.Assignee}` : null,
        data.PlannedDueDate ? `${t("field.plannedDueDate")} ${formatDate(data.PlannedDueDate, lang)}` : null,
      ]
        .filter(Boolean)
        .join(" · ");
    case "Rescheduled":
      return `${t("field.plannedDueDate")} ${formatDate(data.OldDueDate, lang)} → ${formatDate(data.NewDueDate, lang)}`;
    case "Reassigned":
      return `${data.Old ?? "-"} → ${data.New ?? "-"}`;
    case "WatcherAdded":
    case "WatcherRemoved":
      return data.User ?? null;
    default:
      return null;
  }
}

/** Every status change and edit, oldest first (plan 4.3). */
export default function Timeline({ activities }: { activities: Activity[] }) {
  const { t, i18n } = useTranslation("ticket");

  return (
    <Stack spacing={0}>
      {activities.map((activity, index) => {
        const line = detail(activity, parse(activity.Data), t, i18n.language);
        return (
          <Stack key={activity.Id} direction="row" spacing={1.5}>
            <Stack sx={{ alignItems: "center", pt: 0.75 }}>
              <Box sx={{ width: 10, height: 10, borderRadius: "50%", bgcolor: color.accent.brand, flexShrink: 0 }} />
              {index < activities.length - 1 ? <Box sx={{ width: 2, flex: 1, bgcolor: color.border.subtle, my: 0.5 }} /> : null}
            </Stack>
            <Stack sx={{ pb: 2, minWidth: 0 }}>
              <Typography sx={{ ...typeScale.bodyStrong16, color: color.text.primary }}>{t(`activity.${activity.Type}`)}</Typography>
              {line ? <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>{line}</Typography> : null}
              {activity.Note ? (
                <Typography sx={{ ...typeScale.bodySm14, color: color.text.primary, whiteSpace: "pre-wrap", wordBreak: "break-word" }}>{activity.Note}</Typography>
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
