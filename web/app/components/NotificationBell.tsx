"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import Badge from "@mui/material/Badge";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import Popover from "@mui/material/Popover";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import NotificationsNoneRounded from "@mui/icons-material/NotificationsNoneRounded";
import { formatDate, formatDateTime } from "@/lib/format";
import { markAllReadApi, markReadApi, notificationsApi, unreadCountApi, type AppNotification } from "@/lib/notification.api";
import { TICKET_LISTS_KEY } from "@/queries/ticket.query";
import { color, radiusPx, typeScale } from "@/theme/tokens";

const NOTIFICATIONS_KEY = ["notifications"] as const;

type Data = {
  TicketNo?: string;
  Title?: string;
  Assignee?: string;
  PlannedDueDate?: string;
  NewDueDate?: string;
  CloseDate?: string;
  Count?: number;
  RequestedPriority?: string;
};

function parse(data: string | null): Data {
  try {
    return data ? (JSON.parse(data) as Data) : {};
  } catch {
    return {};
  }
}

/** The bell (plan 7): checks every minute; the list loads when opened. */
export default function NotificationBell() {
  const { t, i18n } = useTranslation("notification");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);

  const unread = useQuery({
    queryKey: [...NOTIFICATIONS_KEY, "unread"],
    queryFn: async () => (await unreadCountApi()).Result,
    refetchInterval: 60_000,
  });
  const list = useQuery({
    queryKey: [...NOTIFICATIONS_KEY, "list"],
    queryFn: async () => (await notificationsApi()).Result,
    enabled: !!anchor,
  });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY });
    // A new notification usually means a list changed too.
    void queryClient.invalidateQueries({ queryKey: TICKET_LISTS_KEY });
  };

  const open = async (item: AppNotification) => {
    setAnchor(null);
    if (!item.IsRead) {
      await markReadApi(item.Id).catch(() => undefined);
      refresh();
    }
    if (item.TicketId) router.push(`/Tickets/Detail/?id=${item.TicketId}`);
    else if (item.Type === "PendingApprovalReminder") router.push("/Approvals/");
  };

  const text = (item: AppNotification) => {
    const data = parse(item.Data);
    const lang = i18n.language;
    return t(item.Type, {
      actor: item.ActorName ?? t("system"),
      no: data.TicketNo ?? "",
      assignee: data.Assignee ?? "",
      due: formatDate(data.PlannedDueDate ?? data.NewDueDate, lang),
      date: formatDate(data.CloseDate, lang),
      count: data.Count ?? 0,
    });
  };

  return (
    <>
      <IconButton
        aria-label={t("title")}
        onClick={(e) => {
          setAnchor(e.currentTarget);
          refresh();
        }}
      >
        <Badge badgeContent={unread.data ?? 0} color="error" max={99}>
          <NotificationsNoneRounded />
        </Badge>
      </IconButton>
      <Popover
        open={!!anchor}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
        slotProps={{ paper: { sx: { width: 380, maxWidth: "calc(100vw - 32px)", borderRadius: radiusPx.md } } }}
      >
        <Stack direction="row" sx={{ px: 4, py: 3, alignItems: "center", justifyContent: "space-between" }}>
          <Typography sx={{ ...typeScale.bodyStrong }}>{t("title")}</Typography>
          <Button
            size="small"
            variant="text"
            disabled={!unread.data}
            onClick={() => void markAllReadApi().then(refresh).catch(() => undefined)}
          >
            {t("readAll")}
          </Button>
        </Stack>
        <Divider />
        <Box sx={{ maxHeight: 420, overflowY: "auto" }}>
          {list.data?.length === 0 ? (
            <Typography sx={{ ...typeScale.small, color: color.text.secondary, p: 4, textAlign: "center" }}>{t("empty")}</Typography>
          ) : null}
          {(list.data ?? []).map((item) => {
            const data = parse(item.Data);
            return (
              <Box
                key={item.Id}
                onClick={() => void open(item)}
                sx={{
                  px: 4,
                  py: 2.5,
                  cursor: "pointer",
                  bgcolor: item.IsRead ? undefined : color.surface.brandSubtle,
                  borderBottom: `1px solid ${color.border.subtle}`,
                  "&:hover": { bgcolor: color.surface.sunken },
                }}
              >
                <Typography sx={{ ...typeScale.small, color: color.text.primary, fontWeight: item.IsRead ? 400 : 600 }}>
                  {text(item)}
                  {data.RequestedPriority === "Immediate" ? ` · ${t("immediate")}` : ""}
                </Typography>
                {data.Title ? (
                  <Typography sx={{ ...typeScale.caption, color: color.text.secondary }} noWrap>
                    {data.Title}
                  </Typography>
                ) : null}
                <Typography sx={{ ...typeScale.caption, color: color.text.tertiary }}>{formatDateTime(item.CreatedAt, i18n.language)}</Typography>
              </Box>
            );
          })}
        </Box>
      </Popover>
    </>
  );
}
