"use client";

import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import EditRounded from "@mui/icons-material/EditRounded";
import AppShell from "@/app/components/AppShell";
import Link from "@/app/components/Link";
import { addTicketFilesApi } from "@/lib/ticket.api";
import { formatDate, formatDateTime } from "@/lib/format";
import { ticketKey, useTicketQuery } from "@/queries/ticket.query";
import { useAuthStore } from "@/stores/auth.store";
import type { TicketDetail } from "@/types/ticket";
import { color, radiusPx, typeScale } from "@/theme/tokens";
import AttachmentList from "../_components/AttachmentList";
import Comments from "../_components/Comments";
import FilePicker from "../_components/FilePicker";
import { PriorityChip, StatusChip } from "../_components/StatusChip";
import Timeline from "../_components/Timeline";

function Card({ title, children }: { title?: string; children: React.ReactNode }) {
  return (
    <Stack spacing={2} sx={{ p: { xs: 2, md: 3 }, bgcolor: color.surface.card, border: `1px solid ${color.border.subtle}`, borderRadius: radiusPx.md }}>
      {title ? <Typography sx={{ ...typeScale.h3_20, color: color.text.primary }}>{title}</Typography> : null}
      {children}
    </Stack>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <Stack spacing={0.25} sx={{ minWidth: 0 }}>
      <Typography sx={{ ...typeScale.caption12, color: color.text.secondary }}>{label}</Typography>
      <Box sx={{ ...typeScale.bodySm14, color: color.text.primary, wordBreak: "break-word" }}>{children}</Box>
    </Stack>
  );
}

function LongText({ label, text }: { label: string; text: string | null }) {
  if (!text) return null;
  return (
    <Stack spacing={0.5}>
      <Typography sx={{ ...typeScale.bodyStrong16, color: color.text.primary }}>{label}</Typography>
      <Typography sx={{ ...typeScale.body16, color: color.text.primary, whiteSpace: "pre-wrap", wordBreak: "break-word" }}>{text}</Typography>
    </Stack>
  );
}

function TicketFiles({ ticket, onChanged }: { ticket: TicketDetail; onChanged: (ticket: TicketDetail) => void }) {
  const { t } = useTranslation("ticket");
  const [files, setFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);

  const upload = async () => {
    setUploading(true);
    try {
      const res = await addTicketFilesApi(ticket.Id, files);
      setFiles([]);
      onChanged(res.Result);
      toast.success(t("files.uploaded"));
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setUploading(false);
    }
  };

  return (
    <Stack spacing={1.5}>
      <AttachmentList ticketId={ticket.Id} attachments={ticket.Attachments} showEmpty />
      {ticket.AllowedActions.includes("AddAttachment") ? (
        <Stack spacing={1}>
          <FilePicker files={files} onChange={setFiles} disabled={uploading} />
          {files.length ? (
            <Box>
              <Button variant="contained" size="small" onClick={() => void upload()} disabled={uploading}>
                {t("files.add")}
              </Button>
            </Box>
          ) : null}
        </Stack>
      ) : null}
    </Stack>
  );
}

function TicketView() {
  const { t, i18n } = useTranslation("ticket");
  const { t: tc } = useTranslation("common");
  const queryClient = useQueryClient();
  const userId = useAuthStore((s) => s.user?.UserId);
  const id = Number(useSearchParams().get("id")) || null;
  const query = useTicketQuery(id);

  if (query.isError || !id) return <Typography sx={{ color: color.text.secondary }}>{t("notFound")}</Typography>;
  if (!query.data) return <Typography sx={{ color: color.text.secondary }}>{tc("loading")}</Typography>;

  const ticket = query.data;
  const lang = i18n.language;
  const onChanged = (next: TicketDetail) => queryClient.setQueryData(ticketKey(next.Id), next);
  const person = (name: string | undefined, legacy: string | null) => name ?? legacy ?? t("none");

  return (
    <Stack spacing={3}>
      <Stack spacing={1}>
        <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>
          {ticket.TicketNo}
          {ticket.LegacyRef ? ` · ${t("field.legacyRef")} ${ticket.LegacyRef}` : ""}
        </Typography>
        <Stack direction={{ xs: "column", md: "row" }} spacing={2} sx={{ justifyContent: "space-between", alignItems: { md: "flex-start" } }}>
          <Typography component="h1" sx={{ ...typeScale.h2_24, color: color.text.primary, wordBreak: "break-word" }}>
            {ticket.Title}
          </Typography>
          {ticket.AllowedActions.includes("Edit") ? (
            <Button component={Link} href={`/Tickets/Edit/?id=${ticket.Id}`} variant="outlined" startIcon={<EditRounded />} sx={{ flexShrink: 0 }}>
              {t("edit")}
            </Button>
          ) : null}
        </Stack>
        <Stack direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
          <StatusChip status={ticket.Status} />
          <PriorityChip priority={ticket.ActualPriority ?? ticket.RequestedPriority} />
          <Chip size="small" label={t(`type.${ticket.Type}`)} />
          {ticket.NeedsLegacyReview ? <Chip size="small" color="warning" label={t("legacyReview")} /> : null}
        </Stack>
      </Stack>

      <Box sx={{ display: "grid", gap: 3, gridTemplateColumns: { xs: "1fr", lg: "minmax(0, 2fr) minmax(0, 1fr)" }, alignItems: "start" }}>
        <Stack spacing={3}>
          <Card>
            <LongText label={t("field.description")} text={ticket.Description} />
            <LongText label={t("field.testedDetails")} text={ticket.TestedDetails} />
            <LongText label={t("field.resolution")} text={ticket.Resolution} />
            <Stack spacing={0.5}>
              <Typography sx={{ ...typeScale.bodyStrong16, color: color.text.primary }}>{t("field.attachments")}</Typography>
              <TicketFiles ticket={ticket} onChanged={onChanged} />
            </Stack>
          </Card>

          <Card title={t("section.comments")}>
            <Comments ticket={ticket} isReporter={ticket.Reporter.Id === userId} onChanged={onChanged} />
          </Card>
        </Stack>

        <Stack spacing={3}>
          <Card title={t("section.info")}>
            <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}>
              <Field label={t("field.product")}>{ticket.Product.Name}</Field>
              <Field label={t("field.version")}>{ticket.Version ?? t("none")}</Field>
              <Field label={t("field.customerName")}>{ticket.CustomerName ?? t("none")}</Field>
              <Field label={t("field.deviceModels")}>{ticket.DeviceModels.map((m) => m.Name).join(", ") || t("none")}</Field>
              <Field label={t("field.requestedPriority")}>{t(`priority.${ticket.RequestedPriority}`)}</Field>
              <Field label={t("field.actualPriority")}>{ticket.ActualPriority ? t(`priority.${ticket.ActualPriority}`) : t("none")}</Field>
            </Box>
          </Card>

          <Card title={t("section.people")}>
            <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}>
              <Field label={t("field.reporter")}>{person(ticket.Reporter.DisplayName, ticket.LegacyReporterName)}</Field>
              <Field label={t("field.department")}>{ticket.Department.Name}</Field>
              <Field label={t("field.requester")}>{person(ticket.Requester?.DisplayName, ticket.LegacyRequesterName)}</Field>
              <Field label={t("field.approver")}>{person(ticket.Approver?.DisplayName, ticket.LegacyApproverName)}</Field>
              <Field label={t("field.assignee")}>{person(ticket.Assignee?.DisplayName, ticket.LegacyAssigneeName)}</Field>
              <Field label={t("field.watchers")}>{ticket.Watchers.map((w) => w.DisplayName).join(", ") || t("none")}</Field>
            </Box>
          </Card>

          <Card title={t("section.dates")}>
            <Box sx={{ display: "grid", gap: 2, gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}>
              <Field label={t("field.reportedAt")}>{formatDateTime(ticket.ReportedAt, lang)}</Field>
              <Field label={t("field.approvedAt")}>{formatDateTime(ticket.ApprovedAt, lang)}</Field>
              <Field label={t("field.plannedStartDate")}>{formatDate(ticket.PlannedStartDate, lang)}</Field>
              <Field label={t("field.plannedDueDate")}>{formatDate(ticket.PlannedDueDate, lang)}</Field>
              <Field label={t("field.startedAt")}>{formatDateTime(ticket.StartedAt, lang)}</Field>
              <Field label={t("field.resolvedAt")}>{formatDateTime(ticket.ResolvedAt, lang)}</Field>
              <Field label={t("field.closedAt")}>{formatDateTime(ticket.ClosedAt, lang)}</Field>
            </Box>
          </Card>

          <Card title={t("section.timeline")}>
            <Timeline activities={ticket.Activities} />
          </Card>
        </Stack>
      </Box>
    </Stack>
  );
}

export default function TicketDetailPage() {
  return (
    <AppShell>
      <Suspense>
        <TicketView />
      </Suspense>
    </AppShell>
  );
}
