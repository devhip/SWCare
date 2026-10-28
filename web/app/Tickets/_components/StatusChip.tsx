"use client";

import Chip from "@mui/material/Chip";
import { useTranslation } from "react-i18next";
import type { TicketPriority, TicketStatus } from "@/types/ticket";
import { color } from "@/theme/tokens";

const STATUS_COLORS: Record<TicketStatus, { bg: string; fg: string }> = {
  PendingApproval: { bg: color.semantic.warning.bg, fg: color.semantic.warning.text },
  Rejected: { bg: color.semantic.error.bg, fg: color.semantic.error.text },
  PendingTriage: { bg: color.semantic.warning.bg, fg: color.semantic.warning.text },
  WaitingInfo: { bg: color.accent.violetBg, fg: color.accent.violet },
  Queued: { bg: color.semantic.info.bg, fg: color.semantic.info.text },
  InProgress: { bg: color.accent.blueBg, fg: color.accent.blue },
  Resolved: { bg: color.accent.tealBg, fg: color.accent.teal },
  Closed: { bg: color.semantic.success.bg, fg: color.semantic.success.text },
  Cancelled: { bg: color.surface.sunken, fg: color.text.secondary },
  Declined: { bg: color.surface.sunken, fg: color.text.secondary },
};

export function StatusChip({ status }: { status: TicketStatus }) {
  const { t } = useTranslation("ticket");
  const tone = STATUS_COLORS[status];
  return <Chip label={t(`status.${status}`)} size="small" sx={{ bgcolor: tone.bg, color: tone.fg, fontWeight: 600 }} />;
}

export function PriorityChip({ priority }: { priority: TicketPriority }) {
  const { t } = useTranslation("ticket");
  const tone =
    priority === "Immediate"
      ? { bg: color.semantic.error.bg, fg: color.semantic.error.text }
      : priority === "Urgent"
        ? { bg: color.semantic.warning.bg, fg: color.semantic.warning.text }
        : { bg: color.surface.sunken, fg: color.text.secondary };
  return <Chip label={t(`priority.${priority}`)} size="small" sx={{ bgcolor: tone.bg, color: tone.fg }} />;
}
