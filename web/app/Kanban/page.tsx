"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import { DndContext, DragOverlay, PointerSensor, useDraggable, useDroppable, useSensor, useSensors, type DragEndEvent } from "@dnd-kit/core";
import Box from "@mui/material/Box";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AppShell from "@/app/components/AppShell";
import { TextDialog, TriageDialog } from "@/app/Tickets/_components/TicketActions";
import { PriorityChip } from "@/app/Tickets/_components/StatusChip";
import { formatDate } from "@/lib/format";
import { reorderQueueApi, ticketActionApi, ticketApi } from "@/lib/ticket.api";
import { TICKET_LISTS_KEY, useBoardQuery } from "@/queries/ticket.query";
import { useAuthStore } from "@/stores/auth.store";
import type { BoardCard, TicketAction, TicketDetail, TicketStatus } from "@/types/ticket";
import { color, radiusPx, typeScale } from "@/theme/tokens";

/** Columns = open statuses (Q30). */
const COLUMNS: TicketStatus[] = ["PendingTriage", "Queued", "InProgress", "WaitingInfo", "Resolved"];

/** A drag to another column of the same lane is one of these actions. */
const MOVES: Record<string, TicketAction> = {
  "Queued>InProgress": "Start",
  "InProgress>Queued": "Pause",
  "InProgress>Resolved": "Resolve",
  "InProgress>WaitingInfo": "RequestInfo",
  "PendingTriage>WaitingInfo": "RequestInfo",
};
const DRAG_ACTIONS = new Set<TicketAction>(["Start", "Pause", "Resolve", "RequestInfo", "Triage", "Reassign"]);

/** null = the "not triaged" lane. */
type Lane = number | null;
type Target = { lane: Lane; status: TicketStatus; beforeId?: number };
type Pending =
  | { kind: "triage"; ticket: TicketDetail; assigneeId: number }
  | { kind: "text"; action: "Resolve" | "RequestInfo"; card: BoardCard };

const cellId = (lane: Lane, status: TicketStatus) => `cell:${lane ?? "none"}:${status}`;

function parseTarget(id: string, cards: BoardCard[]): Target | null {
  if (id.startsWith("card:")) {
    const card = cards.find((c) => c.Id === Number(id.slice(5)));
    return card ? { lane: card.AssigneeId, status: card.Status, beforeId: card.Id } : null;
  }
  const [, lane, status] = id.split(":");
  return { lane: lane === "none" ? null : Number(lane), status: status as TicketStatus };
}

function CardView({ card, dragging }: { card: BoardCard; dragging?: boolean }) {
  const { i18n } = useTranslation();
  return (
    <Stack
      spacing={1}
      sx={{
        p: 3,
        bgcolor: color.surface.card,
        border: `1px solid ${card.IsOverdue ? color.semantic.error.border : "transparent"}`,
        borderRadius: "12px",
        boxShadow: dragging ? "0 12px 28px rgba(240, 100, 0, .22)" : "0 2px 8px rgba(43, 26, 14, .07)",
      }}
    >
      <Typography sx={{ ...typeScale.caption, color: color.text.secondary }}>{card.TicketNo}</Typography>
      <Typography sx={{ ...typeScale.small, fontWeight: 600, color: color.text.primary, display: "-webkit-box", WebkitLineClamp: 2, WebkitBoxOrient: "vertical", overflow: "hidden" }}>
        {card.Title}
      </Typography>
      <Typography sx={{ ...typeScale.caption, color: color.text.secondary }} noWrap>
        {[card.ProductName, card.CustomerName].filter(Boolean).join(" · ")}
      </Typography>
      <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between", gap: 1 }}>
        <PriorityChip priority={card.ActualPriority ?? card.RequestedPriority} />
        {card.PlannedDueDate ? (
          <Typography sx={{ ...typeScale.caption, color: card.IsOverdue ? color.semantic.error.text : color.text.secondary, fontWeight: card.IsOverdue ? 600 : 400 }}>
            {formatDate(card.PlannedDueDate, i18n.language)}
          </Typography>
        ) : null}
      </Stack>
    </Stack>
  );
}

function DraggableCard({ card, canDrag }: { card: BoardCard; canDrag: boolean }) {
  const router = useRouter();
  const drag = useDraggable({ id: `card:${card.Id}`, disabled: !canDrag });
  const drop = useDroppable({ id: `card:${card.Id}` });
  return (
    <Box
      ref={(node: HTMLElement | null) => {
        drag.setNodeRef(node);
        drop.setNodeRef(node);
      }}
      {...drag.listeners}
      {...drag.attributes}
      onClick={() => router.push(`/Tickets/Detail/?id=${card.Id}`)}
      sx={{
        opacity: drag.isDragging ? 0.35 : 1,
        cursor: canDrag ? "grab" : "pointer",
        outline: drop.isOver && !drag.isDragging ? `2px solid ${color.accent.brand}` : "none",
        borderRadius: radiusPx.sm,
        touchAction: "none",
      }}
    >
      <CardView card={card} />
    </Box>
  );
}

function Cell({ lane, status, children }: { lane: Lane; status: TicketStatus; children: React.ReactNode }) {
  const { setNodeRef, isOver } = useDroppable({ id: cellId(lane, status) });
  return (
    <Stack
      ref={setNodeRef}
      spacing={2}
      sx={{
        p: 2,
        minHeight: 96,
        // a visible lane on the cream page (HIPHR inline-form tint)
        bgcolor: isOver ? color.surface.brandSubtle : "#FCEFE3",
        borderRadius: radiusPx.card,
        outline: isOver ? `2px dashed ${color.brand[300]}` : "none",
      }}
    >
      {children}
    </Stack>
  );
}

function KanbanBoard() {
  const { t } = useTranslation("ticket");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const user = useAuthStore((s) => s.user);
  const board = useBoardQuery();
  const [activeId, setActiveId] = useState<number | null>(null);
  const [pending, setPending] = useState<Pending | null>(null);
  const [busy, setBusy] = useState(false);
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 6 } }));

  if (!board.data) return <Typography sx={{ color: color.text.secondary }}>{tc("loading")}</Typography>;

  const { Programmers: programmers, Cards: cards } = board.data;
  const isLead = !!user?.IsProgrammerLead;
  const lanes: { id: Lane; name: string }[] = [{ id: null, name: t("kanban.unassigned") }, ...programmers.map((p) => ({ id: p.Id, name: p.DisplayName }))];
  const cardsIn = (lane: Lane, status: TicketStatus) =>
    cards.filter((c) => c.AssigneeId === lane && c.Status === status).sort((a, b) => a.QueueOrder - b.QueueOrder || a.Id - b.Id);
  const canDrag = (card: BoardCard) => card.AllowedActions.some((a) => DRAG_ACTIONS.has(a)) || (isLead && card.Status === "Queued");

  const refresh = () => void queryClient.invalidateQueries({ queryKey: TICKET_LISTS_KEY });

  const run = async (id: number, action: TicketAction, body?: unknown) => {
    setBusy(true);
    try {
      await ticketActionApi(id, action, body);
      toast.success(t("action.done"));
      setPending(null);
      refresh();
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setBusy(false);
    }
  };

  const notAllowed = () => toast.info(t("kanban.notAllowed"));

  const onDragEnd = async ({ active, over }: DragEndEvent) => {
    setActiveId(null);
    if (!over) return;
    const card = cards.find((c) => `card:${c.Id}` === active.id);
    const target = parseTarget(String(over.id), cards);
    if (!card || !target) return;
    const can = (action: TicketAction) => card.AllowedActions.includes(action);
    const sameLane = target.lane === card.AssigneeId;
    const sameStatus = target.status === card.Status;

    // Up and down in a queue: only the lead (Q30).
    if (sameLane && sameStatus) {
      if (card.Status !== "Queued" || !target.beforeId || target.beforeId === card.Id || card.AssigneeId === null) return;
      if (!isLead) return notAllowed();
      const ids = cardsIn(card.AssigneeId, "Queued").map((c) => c.Id).filter((id) => id !== card.Id);
      ids.splice(ids.indexOf(target.beforeId), 0, card.Id);
      try {
        await reorderQueueApi(card.AssigneeId, ids);
        refresh();
      } catch {
        // The error toast is shown by lib/http.
      }
      return;
    }

    // From "not triaged" into a programmer's lane = triage for that programmer.
    if (card.Status === "PendingTriage" && target.lane !== null) {
      if (!can("Triage")) return notAllowed();
      const ticket = (await ticketApi(card.Id)).Result;
      setPending({ kind: "triage", ticket, assigneeId: target.lane });
      return;
    }

    // Another lane, same column = another assignee (lead only).
    if (!sameLane && sameStatus && target.lane !== null && card.AssigneeId !== null) {
      return can("Reassign") ? void run(card.Id, "Reassign", { AssigneeId: target.lane }) : notAllowed();
    }

    const action = sameLane ? MOVES[`${card.Status}>${target.status}`] : undefined;
    if (!action || !can(action)) return notAllowed();
    if (action === "Resolve" || action === "RequestInfo") setPending({ kind: "text", action, card });
    else void run(card.Id, action);
  };

  const active = cards.find((c) => c.Id === activeId);

  return (
    <>
      <DndContext
        sensors={sensors}
        onDragStart={({ active: dragged }) => setActiveId(Number(String(dragged.id).slice(5)))}
        onDragCancel={() => setActiveId(null)}
        onDragEnd={(event) => void onDragEnd(event)}
      >
        <Box sx={{ overflowX: "auto", pb: 2 }}>
          <Box sx={{ display: "grid", gridTemplateColumns: `140px repeat(${COLUMNS.length}, minmax(220px, 1fr))`, gap: 2, minWidth: 140 + COLUMNS.length * 228 }}>
            <Box />
            {COLUMNS.map((status) => (
              <Typography key={status} sx={{ ...typeScale.bodyStrong, color: color.text.primary, px: 1 }}>
                {t(`status.${status}`)}
              </Typography>
            ))}
            {lanes.map((lane) => (
              <Box key={lane.id ?? "none"} sx={{ display: "contents" }}>
                <Typography sx={{ ...typeScale.bodyStrong, color: color.text.secondary, pt: 2, wordBreak: "break-word" }}>{lane.name}</Typography>
                {COLUMNS.map((status) => (
                  <Cell key={status} lane={lane.id} status={status}>
                    {cardsIn(lane.id, status).map((card) => (
                      <DraggableCard key={card.Id} card={card} canDrag={canDrag(card) && !busy} />
                    ))}
                  </Cell>
                ))}
              </Box>
            ))}
          </Box>
        </Box>
        <DragOverlay>{active ? <CardView card={active} dragging /> : null}</DragOverlay>
      </DndContext>

      {pending?.kind === "triage" ? (
        <TriageDialog
          ticket={pending.ticket}
          initialAssigneeId={pending.assigneeId}
          busy={busy}
          onClose={() => setPending(null)}
          onSubmit={(body) => void run(pending.ticket.Id, "Triage", body)}
        />
      ) : null}
      {pending?.kind === "text" ? (
        <TextDialog
          action={pending.action}
          label={pending.action === "Resolve" ? t("dialog.resolution") : t("dialog.reason.RequestInfo")}
          field={pending.action === "Resolve" ? "Resolution" : "Reason"}
          rows={pending.action === "Resolve" ? 5 : 3}
          busy={busy}
          onClose={() => setPending(null)}
          onSubmit={(action, body) => void run(pending.card.Id, action, body)}
        />
      ) : null}
    </>
  );
}

export default function KanbanPage() {
  const { t } = useTranslation("navigation");
  const { t: tt } = useTranslation("ticket");
  return (
    <AppShell roles={["Programmer"]}>
      <Stack spacing={5}>
        <Stack spacing={0.5}>
          <Typography component="h1" sx={{ ...typeScale.pageTitle, color: color.text.primary }}>
            {t("kanban")}
          </Typography>
          <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{tt("kanban.hint")}</Typography>
        </Stack>
        <KanbanBoard />
      </Stack>
    </AppShell>
  );
}
