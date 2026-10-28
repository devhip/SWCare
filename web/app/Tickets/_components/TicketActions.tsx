"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Autocomplete from "@mui/material/Autocomplete";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import { ticketActionApi } from "@/lib/ticket.api";
import { addDays, thaiToday } from "@/lib/format";
import { TICKET_LISTS_KEY, useProgrammersQuery, useTicketFormDataQuery } from "@/queries/ticket.query";
import { TICKET_PRIORITIES, TICKET_TYPES, type TicketAction, type TicketDetail, type TicketPriority, type TicketType } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";

/** Buttons in this order; Edit, Comment, attachments and watchers have their own places on the page. */
const ORDER: TicketAction[] = ["Approve", "Reject", "Resubmit", "Triage", "Start", "Resolve", "Close", "SendBack", "Pause", "RequestInfo", "Reschedule", "Reassign", "Decline", "Cancel"];
const PRIMARY = new Set<TicketAction>(["Approve", "Resubmit", "Triage", "Start", "Resolve", "Close"]);
const DANGER = new Set<TicketAction>(["Reject", "Decline", "Cancel"]);
const WITH_REASON = new Set<TicketAction>(["Reject", "Cancel", "Decline", "SendBack", "RequestInfo"]);
const NO_INPUT = new Set<TicketAction>(["Approve", "Resubmit", "Start", "Pause"]);

/** First guess of the due date from the priority; the lead changes it (Q21). */
const DUE_DAYS: Record<TicketPriority, number> = { Immediate: 0, Urgent: 2, Normal: 7 };

type Props = { ticket: TicketDetail; onChanged: (ticket: TicketDetail) => void };

export default function TicketActions({ ticket, onChanged }: Props) {
  const { t } = useTranslation("ticket");
  const queryClient = useQueryClient();
  const [open, setOpen] = useState<TicketAction | null>(null);
  const [busy, setBusy] = useState(false);

  const actions = ORDER.filter((action) => ticket.AllowedActions.includes(action));
  if (!actions.length) return null;

  const run = async (action: TicketAction, body?: unknown) => {
    setBusy(true);
    try {
      const res = await ticketActionApi(ticket.Id, action, body);
      onChanged(res.Result);
      void queryClient.invalidateQueries({ queryKey: TICKET_LISTS_KEY });
      toast.success(t("action.done"));
      setOpen(null);
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
        {actions.map((action) => (
          <Button
            key={action}
            variant={PRIMARY.has(action) ? "contained" : "outlined"}
            color={DANGER.has(action) ? "error" : "primary"}
            disabled={busy}
            onClick={() => (NO_INPUT.has(action) ? void run(action) : setOpen(action))}
          >
            {t(`action.${action}`)}
          </Button>
        ))}
      </Stack>

      {open && WITH_REASON.has(open) ? (
        <TextDialog action={open} label={t(`dialog.reason.${open}`)} field="Reason" busy={busy} onClose={() => setOpen(null)} onSubmit={run} />
      ) : null}
      {open === "Resolve" ? (
        <TextDialog action="Resolve" label={t("dialog.resolution")} field="Resolution" rows={5} busy={busy} onClose={() => setOpen(null)} onSubmit={run} />
      ) : null}
      {open === "Close" ? <CloseDialog busy={busy} onClose={() => setOpen(null)} onSubmit={() => run("Close")} /> : null}
      {open === "Triage" ? <TriageDialog ticket={ticket} busy={busy} onClose={() => setOpen(null)} onSubmit={(body) => run("Triage", body)} /> : null}
      {open === "Reschedule" ? <RescheduleDialog ticket={ticket} busy={busy} onClose={() => setOpen(null)} onSubmit={(body) => run("Reschedule", body)} /> : null}
      {open === "Reassign" ? <ReassignDialog ticket={ticket} busy={busy} onClose={() => setOpen(null)} onSubmit={(body) => run("Reassign", body)} /> : null}
    </>
  );
}

type DialogProps = { busy: boolean; onClose: () => void };

function DialogFrame({ title, busy, onClose, onSubmit, canSubmit, children }: DialogProps & { title: string; onSubmit: () => void; canSubmit: boolean; children: React.ReactNode }) {
  const { t } = useTranslation("ticket");
  const { t: tc } = useTranslation("common");
  return (
    // The theme sets the sm breakpoint to 0 (Easy Next), so maxWidth="sm" would be 0 px wide.
    <Dialog open onClose={busy ? undefined : onClose} maxWidth={false} slotProps={{ paper: { sx: { width: "100%", maxWidth: 640 } } }}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {children}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button variant="outlined" onClick={onClose} disabled={busy}>
          {tc("cancel")}
        </Button>
        <Button variant="contained" onClick={onSubmit} disabled={busy || !canSubmit}>
          {t("action.confirm")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export function TextDialog({
  action,
  label,
  field,
  rows = 3,
  busy,
  onClose,
  onSubmit,
}: DialogProps & { action: TicketAction; label: string; field: "Reason" | "Resolution"; rows?: number; onSubmit: (action: TicketAction, body: unknown) => void }) {
  const { t } = useTranslation("ticket");
  const [text, setText] = useState("");
  return (
    <DialogFrame title={t(`action.${action}`)} busy={busy} onClose={onClose} canSubmit={!!text.trim()} onSubmit={() => onSubmit(action, { [field]: text })}>
      <TextField label={label} required multiline minRows={rows} fullWidth autoFocus value={text} onChange={(e) => setText(e.target.value)} />
    </DialogFrame>
  );
}

function CloseDialog({ busy, onClose, onSubmit }: DialogProps & { onSubmit: () => void }) {
  const { t } = useTranslation("ticket");
  return (
    <DialogFrame title={t("action.Close")} busy={busy} onClose={onClose} canSubmit onSubmit={onSubmit}>
      <Typography sx={{ ...typeScale.body16, color: color.text.primary }}>{t("action.closeConfirm")}</Typography>
    </DialogFrame>
  );
}

function DateFields({ start, due, onStart, onDue }: { start: string; due: string; onStart: (v: string) => void; onDue: (v: string) => void }) {
  const { t } = useTranslation("ticket");
  return (
    <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
      <TextField type="date" label={t("field.plannedStartDate")} value={start} onChange={(e) => onStart(e.target.value)} fullWidth slotProps={{ inputLabel: { shrink: true } }} />
      <TextField
        type="date"
        label={t("field.plannedDueDate")}
        value={due}
        onChange={(e) => onDue(e.target.value)}
        fullWidth
        error={!!start && !!due && due < start}
        slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: start } }}
      />
    </Stack>
  );
}

function ProgrammerSelect({ value, onChange }: { value: number | ""; onChange: (id: number) => void }) {
  const { t } = useTranslation("ticket");
  const programmers = useProgrammersQuery(true);
  return (
    <TextField select label={t("field.assignee")} required fullWidth value={programmers.data ? value : ""} onChange={(e) => onChange(Number(e.target.value))}>
      {(programmers.data ?? []).map((user) => (
        <MenuItem key={user.Id} value={user.Id}>
          {user.DisplayName}
        </MenuItem>
      ))}
    </TextField>
  );
}

export function TriageDialog({
  ticket,
  initialAssigneeId,
  busy,
  onClose,
  onSubmit,
}: DialogProps & { ticket: TicketDetail; initialAssigneeId?: number; onSubmit: (body: unknown) => void }) {
  const { t } = useTranslation("ticket");
  const formData = useTicketFormDataQuery();
  const today = thaiToday();
  const [priority, setPriority] = useState<TicketPriority>(ticket.RequestedPriority);
  // The product's main programmer is suggested (Q3, Q19).
  const [assigneeId, setAssigneeId] = useState<number | "">(initialAssigneeId ?? "");
  const [start, setStart] = useState(today);
  const [due, setDue] = useState(addDays(today, DUE_DAYS[ticket.RequestedPriority]));
  const [productId, setProductId] = useState(ticket.Product.Id);
  const [type, setType] = useState<TicketType>(ticket.Type);
  const [modelIds, setModelIds] = useState(ticket.DeviceModels.map((m) => m.Id));
  const [customer, setCustomer] = useState(ticket.CustomerName ?? "");

  const owner = formData.data?.products.find((p) => p.Id === productId)?.OwnerUserId ?? "";
  const effectiveAssignee = assigneeId === "" ? owner : assigneeId;

  return (
    <DialogFrame
      title={t("action.Triage")}
      busy={busy}
      onClose={onClose}
      canSubmit={effectiveAssignee !== "" && !!start && !!due && due >= start}
      onSubmit={() =>
        onSubmit({
          ActualPriority: priority,
          AssigneeId: effectiveAssignee,
          PlannedStartDate: start,
          PlannedDueDate: due,
          ProductId: productId,
          Type: type,
          DeviceModelIds: modelIds,
          CustomerName: customer || null,
        })
      }
    >
      <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>{t("dialog.triageHint")}</Typography>
      <Stack spacing={1}>
        <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>
          {t("field.actualPriority")} · {t("dialog.requested", { priority: t(`priority.${ticket.RequestedPriority}`) })}
        </Typography>
        <ToggleButtonGroup
          exclusive
          color="primary"
          value={priority}
          onChange={(_, value: TicketPriority | null) => {
            if (!value) return;
            setPriority(value);
            setDue(addDays(start, DUE_DAYS[value]));
          }}
          sx={{ flexWrap: "wrap" }}
        >
          {TICKET_PRIORITIES.map((value) => (
            <ToggleButton key={value} value={value} sx={{ px: 2 }}>
              {t(`priority.${value}`)}
            </ToggleButton>
          ))}
        </ToggleButtonGroup>
      </Stack>
      <ProgrammerSelect value={effectiveAssignee} onChange={setAssigneeId} />
      <DateFields start={start} due={due} onStart={setStart} onDue={setDue} />

      <Typography sx={{ ...typeScale.bodyStrong16, color: color.text.primary, pt: 1 }}>{t("dialog.classification")}</Typography>
      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <TextField select label={t("field.type")} fullWidth value={type} onChange={(e) => setType(e.target.value as TicketType)}>
          {TICKET_TYPES.map((value) => (
            <MenuItem key={value} value={value}>
              {t(`type.${value}`)}
            </MenuItem>
          ))}
        </TextField>
        <TextField select label={t("field.product")} fullWidth value={formData.data ? productId : ""} onChange={(e) => setProductId(Number(e.target.value))}>
          {(formData.data?.products ?? []).map((product) => (
            <MenuItem key={product.Id} value={product.Id}>
              {product.Name}
            </MenuItem>
          ))}
        </TextField>
      </Stack>
      <Autocomplete
        multiple
        options={formData.data?.deviceModels ?? []}
        getOptionLabel={(option) => option.Name}
        value={(formData.data?.deviceModels ?? []).filter((m) => modelIds.includes(m.Id))}
        onChange={(_, value) => setModelIds(value.map((m) => m.Id))}
        renderInput={(params) => <TextField {...params} label={t("field.deviceModels")} />}
      />
      <TextField label={t("field.customerName")} fullWidth value={customer} onChange={(e) => setCustomer(e.target.value)} />
    </DialogFrame>
  );
}

function RescheduleDialog({ ticket, busy, onClose, onSubmit }: DialogProps & { ticket: TicketDetail; onSubmit: (body: unknown) => void }) {
  const { t } = useTranslation("ticket");
  const [start, setStart] = useState(ticket.PlannedStartDate ?? thaiToday());
  const [due, setDue] = useState(ticket.PlannedDueDate ?? thaiToday());
  const [reason, setReason] = useState("");
  return (
    <DialogFrame
      title={t("action.Reschedule")}
      busy={busy}
      onClose={onClose}
      canSubmit={!!reason.trim() && !!start && !!due && due >= start}
      onSubmit={() => onSubmit({ PlannedStartDate: start, PlannedDueDate: due, Reason: reason })}
    >
      <DateFields start={start} due={due} onStart={setStart} onDue={setDue} />
      <TextField label={t("dialog.reason.Reschedule")} required multiline minRows={2} fullWidth value={reason} onChange={(e) => setReason(e.target.value)} />
    </DialogFrame>
  );
}

function ReassignDialog({ ticket, busy, onClose, onSubmit }: DialogProps & { ticket: TicketDetail; onSubmit: (body: unknown) => void }) {
  const { t } = useTranslation("ticket");
  const [assigneeId, setAssigneeId] = useState<number | "">(ticket.Assignee?.Id ?? "");
  return (
    <DialogFrame
      title={t("action.Reassign")}
      busy={busy}
      onClose={onClose}
      canSubmit={assigneeId !== "" && assigneeId !== ticket.Assignee?.Id}
      onSubmit={() => onSubmit({ AssigneeId: assigneeId })}
    >
      <ProgrammerSelect value={assigneeId} onChange={setAssigneeId} />
    </DialogFrame>
  );
}
