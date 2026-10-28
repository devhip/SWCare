"use client";

import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import FileDownloadRounded from "@mui/icons-material/FileDownloadRounded";
import AppShell from "@/app/components/AppShell";
import Link from "@/app/components/Link";
import { dashboardApi, exportDashboardApi } from "@/lib/dashboard.api";
import { thaiToday } from "@/lib/format";
import { DASHBOARD_ROLES } from "@/lib/menu";
import { useDepartmentsQuery } from "@/queries/admin.query";
import { useTicketFormDataQuery } from "@/queries/ticket.query";
import { useAuthStore } from "@/stores/auth.store";
import { color, typeScale } from "@/theme/tokens";
import type { Breakdown, Dashboard, DashboardParams } from "@/types/dashboard";
import { formatNumber, HBarList, MonthColumns, Panel, StatTile, type BarItem } from "./_components/Charts";

type Preset = "3m" | "6m" | "12m" | "thisYear" | "lastYear" | "custom";
const PRESETS: Preset[] = ["3m", "6m", "12m", "thisYear", "lastYear", "custom"];

/** Thai-date range of a preset; "Nm" = this month and the N-1 before it. */
function presetRange(preset: Preset, today: string): { from: string; to: string } {
  const [year, month] = today.split("-").map(Number);
  const monthsBack = (n: number) => {
    const d = new Date(Date.UTC(year, month - 1 - (n - 1), 1));
    return d.toISOString().slice(0, 10);
  };
  switch (preset) {
    case "3m":
      return { from: monthsBack(3), to: today };
    case "12m":
      return { from: monthsBack(12), to: today };
    case "thisYear":
      return { from: `${year}-01-01`, to: today };
    case "lastYear":
      return { from: `${year - 1}-01-01`, to: `${year - 1}-12-31` };
    default:
      return { from: monthsBack(6), to: today };
  }
}

function monthLabel(key: string, lang: string) {
  const [year, month] = key.split("-").map(Number);
  return new Intl.DateTimeFormat(lang === "en" ? "en-GB" : "th-TH-u-ca-gregory", { month: "short", year: "2-digit", timeZone: "UTC" }).format(
    new Date(Date.UTC(year, month - 1, 1))
  );
}

function DashboardView() {
  const { t, i18n } = useTranslation("dashboard");
  const { t: tt } = useTranslation("ticket");
  const user = useAuthStore((s) => s.user);
  const seesAll = !!user && (user.IsExecutive || user.IsAdmin || user.IsSupportHead || user.IsProgrammerLead);

  const today = thaiToday();
  const [preset, setPreset] = useState<Preset>("6m");
  const [custom, setCustom] = useState(() => presetRange("6m", today));
  const [departmentId, setDepartmentId] = useState<number | "">("");
  const [productId, setProductId] = useState<number | "">("");
  const [exporting, setExporting] = useState(false);

  const range = preset === "custom" ? custom : presetRange(preset, today);
  const params: DashboardParams = {
    From: range.from,
    To: range.to,
    DepartmentId: seesAll && departmentId !== "" ? departmentId : undefined,
    ProductId: productId === "" ? undefined : productId,
  };
  const dashboard = useQuery({
    queryKey: ["dashboard", params],
    queryFn: async () => (await dashboardApi(params)).Result,
    placeholderData: keepPreviousData,
    enabled: range.from <= range.to,
  });
  const departments = useDepartmentsQuery();
  const formData = useTicketFormDataQuery();

  const download = async () => {
    setExporting(true);
    try {
      await exportDashboardApi(params);
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setExporting(false);
    }
  };

  const data = dashboard.data;
  const ownDepartment = !seesAll ? departments.data?.find((d) => d.Id === user?.DepartmentId)?.Name : undefined;

  return (
    <Stack spacing={5}>
      <Stack direction={{ xs: "column", md: "row" }} spacing={2} sx={{ justifyContent: "space-between", alignItems: { md: "flex-end" } }}>
        <Stack spacing={0.5}>
          <Typography component="h1" sx={{ ...typeScale.pageTitle, color: color.text.primary }}>
            {t("title")}
          </Typography>
          <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>
            {ownDepartment ? t("hintDepartment", { name: ownDepartment }) : t("hint")}
          </Typography>
        </Stack>
        <Button variant="outlined" startIcon={<FileDownloadRounded />} onClick={() => void download()} disabled={exporting || !data} sx={{ flexShrink: 0 }}>
          {exporting ? t("exporting") : t("export")}
        </Button>
      </Stack>

      {/* Filters: one row above the charts */}
      <Stack direction={{ xs: "column", md: "row" }} spacing={3} sx={{ flexWrap: "wrap", rowGap: 3 }}>
        <TextField select size="small" label={t("range.label")} value={preset} onChange={(e) => setPreset(e.target.value as Preset)} sx={{ minWidth: { md: 180 } }}>
          {PRESETS.map((p) => (
            <MenuItem key={p} value={p}>
              {t(`range.${p}`)}
            </MenuItem>
          ))}
        </TextField>
        {preset === "custom" ? (
          <>
            <TextField size="small" type="date" label={t("range.from")} value={custom.from} onChange={(e) => setCustom((c) => ({ ...c, from: e.target.value }))} />
            <TextField size="small" type="date" label={t("range.to")} value={custom.to} onChange={(e) => setCustom((c) => ({ ...c, to: e.target.value }))} error={custom.from > custom.to} />
          </>
        ) : null}
        {seesAll ? (
          <TextField
            select
            size="small"
            label={tt("field.department")}
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value === "" ? "" : Number(e.target.value))}
            slotProps={{ select: { displayEmpty: true } }}
            sx={{ minWidth: { md: 200 } }}
          >
            <MenuItem value="">{t("allDepartments")}</MenuItem>
            {(departments.data ?? []).map((d) => (
              <MenuItem key={d.Id} value={d.Id}>
                {d.Name}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        <TextField
          select
          size="small"
          label={tt("field.product")}
          value={productId}
          onChange={(e) => setProductId(e.target.value === "" ? "" : Number(e.target.value))}
          slotProps={{ select: { displayEmpty: true } }}
          sx={{ minWidth: { md: 240 } }}
        >
          <MenuItem value="">{t("allProducts")}</MenuItem>
          {(formData.data?.products ?? []).map((p) => (
            <MenuItem key={p.Id} value={p.Id}>
              {p.Name}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      {data ? <DashboardBody data={data} /> : <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{i18n.t("common:loading")}</Typography>}
    </Stack>
  );
}

function DashboardBody({ data }: { data: Dashboard }) {
  const { t, i18n } = useTranslation("dashboard");
  const { t: tt } = useTranslation("ticket");
  const kpi = data.Kpi;
  const days = (value: number | null) => (value === null ? t("kpi.none") : t("kpi.days", { value: formatNumber(value) }));
  const bars = (items: Breakdown[], label?: (b: Breakdown) => string): BarItem[] =>
    items.map((b) => ({ key: b.Key, label: label ? label(b) : b.Label, value: b.Count, tooltip: t("tooltip", { count: b.Count, open: b.Open }) }));

  return (
    <Stack spacing={5}>
      <Box sx={{ display: "grid", gap: 4, gridTemplateColumns: { xs: "repeat(2, minmax(0, 1fr))", md: "repeat(3, minmax(0, 1fr))", xl: "repeat(6, minmax(0, 1fr))" } }}>
        <StatTile label={t("kpi.open")} value={formatNumber(kpi.OpenNow)} sub={t("kpi.openSub", { approval: kpi.WaitingApprovalNow, triage: kpi.WaitingTriageNow })} />
        <StatTile label={t("kpi.overdue")} value={formatNumber(kpi.OverdueNow)} sub={t("kpi.overdueSub")} tone={kpi.OverdueNow > 0 ? "warning" : undefined} />
        <StatTile label={t("kpi.new")} value={formatNumber(kpi.NewInRange)} sub={t("kpi.newSub")} />
        <StatTile label={t("kpi.closed")} value={formatNumber(kpi.ClosedInRange)} sub={t("kpi.closedSub", { declined: kpi.DeclinedInRange, cancelled: kpi.CancelledInRange })} />
        <StatTile
          label={t("kpi.onTime")}
          value={kpi.OnTimePercent === null ? t("kpi.none") : `${formatNumber(kpi.OnTimePercent)}%`}
          sub={t("kpi.onTimeSub", { count: kpi.OnTimeCount, base: kpi.OnTimeBase })}
        />
        <StatTile label={t("kpi.avgClose")} value={days(kpi.AvgDaysToClose)} sub={t("kpi.avgCloseSub")} />
      </Box>

      <Box sx={{ display: "grid", gap: 5, gridTemplateColumns: { xs: "minmax(0, 1fr)", lg: "minmax(0, 2fr) minmax(0, 1fr)" }, alignItems: "start" }}>
        <Panel title={t("months.title")} hint={t("months.hint")}>
          <MonthColumns
            points={data.Months.map((m) => ({ key: m.Month, label: monthLabel(m.Month, i18n.language), first: m.New, second: m.Closed }))}
            firstLabel={t("months.new")}
            secondLabel={t("months.closed")}
          />
        </Panel>
        <Panel title={t("openByStatus")}>
          <HBarList
            items={data.OpenByStatus.map((s) => ({ key: s.Status, label: tt(`status.${s.Status}`), value: s.Count }))}
            emptyText={t("empty")}
          />
        </Panel>
      </Box>

      <Box sx={{ display: "grid", gap: 5, gridTemplateColumns: { xs: "minmax(0, 1fr)", lg: "repeat(3, minmax(0, 1fr))" }, alignItems: "start" }}>
        <Panel title={t("byProduct")} hint={t("breakdownHint")}>
          <HBarList items={bars(data.ByProduct)} emptyText={t("empty")} />
        </Panel>
        <Panel title={t("byType")} hint={t("breakdownHint")}>
          <HBarList items={bars(data.ByType, (b) => tt(`type.${b.Key}`))} emptyText={t("empty")} />
        </Panel>
        <Panel title={t("byDepartment")} hint={t("breakdownHint")}>
          <HBarList items={bars(data.ByDepartment)} emptyText={t("empty")} />
        </Panel>
      </Box>

      <Box sx={{ display: "grid", gap: 5, gridTemplateColumns: { xs: "minmax(0, 1fr)", lg: "repeat(2, minmax(0, 1fr))" }, alignItems: "start" }}>
        <Panel title={t("byCustomer")} hint={t("breakdownHint")}>
          <HBarList items={bars(data.ByCustomer)} emptyText={t("empty")} />
        </Panel>
        <Panel title={t("stages.title")} hint={t("stages.hint")}>
          <HBarList
            items={data.Stages.filter((s) => s.AvgDays !== null).map((s) => ({
              key: s.Stage,
              label: t(`stages.${s.Stage}`),
              value: s.AvgDays ?? 0,
              display: days(s.AvgDays),
              tooltip: t("stages.tooltip", { days: formatNumber(s.AvgDays ?? 0), count: s.Count }),
            }))}
            emptyText={t("empty")}
          />
        </Panel>
      </Box>

      <Panel title={t("team.title")} hint={t("team.hint")}>
        {data.Team.length ? (
          <Box sx={{ overflowX: "auto", mx: -3 }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>{t("team.name")}</TableCell>
                  <TableCell align="right">{t("team.open")}</TableCell>
                  <TableCell align="right">{t("team.inProgress")}</TableCell>
                  <TableCell align="right">{t("team.overdue")}</TableCell>
                  <TableCell align="right">{t("team.resolved")}</TableCell>
                  <TableCell align="right">{t("team.avgWork")}</TableCell>
                  <TableCell align="right">{t("team.reschedules")}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {data.Team.map((m) => (
                  <TableRow key={m.UserId} hover>
                    <TableCell sx={{ fontWeight: 600 }}>{m.Name}</TableCell>
                    <TableCell align="right">{m.OpenNow}</TableCell>
                    <TableCell align="right">{m.InProgressNow}</TableCell>
                    <TableCell align="right" sx={{ color: m.OverdueNow > 0 ? color.semantic.error.text : undefined, fontWeight: m.OverdueNow > 0 ? 600 : undefined }}>
                      {m.OverdueNow}
                    </TableCell>
                    <TableCell align="right">{m.ResolvedInRange}</TableCell>
                    <TableCell align="right">{days(m.AvgWorkDays)}</TableCell>
                    <TableCell align="right">{m.Reschedules}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>
        ) : (
          <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{t("empty")}</Typography>
        )}
      </Panel>

      {data.Declines.length ? (
        <Panel title={t("declines.title")} hint={t("declines.hint")}>
          <Box sx={{ display: "grid", gap: 4, gridTemplateColumns: { xs: "minmax(0, 1fr)", md: "repeat(2, minmax(0, 1fr))" } }}>
            {data.Declines.map((group) => (
              <Stack key={group.Department} spacing={2} sx={{ p: 4, borderRadius: "14px", bgcolor: color.surface.sunken }}>
                <Stack direction="row" sx={{ justifyContent: "space-between" }}>
                  <Typography sx={{ ...typeScale.bodyStrong, color: color.text.primary }}>{group.Department}</Typography>
                  <Typography sx={{ ...typeScale.label, color: color.text.secondary }}>{t("declines.count", { count: group.Count })}</Typography>
                </Stack>
                {group.Recent.map((r) => (
                  <Stack key={r.TicketId} spacing={0.25}>
                    <Link href={`/Tickets/Detail/?id=${r.TicketId}`} style={{ ...typeScale.small, fontWeight: 600 }}>
                      {r.TicketNo} · {r.Title}
                    </Link>
                    {r.Reason ? <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{r.Reason}</Typography> : null}
                  </Stack>
                ))}
              </Stack>
            ))}
          </Box>
        </Panel>
      ) : null}
    </Stack>
  );
}

export default function DashboardPage() {
  return (
    <AppShell roles={DASHBOARD_ROLES}>
      <DashboardView />
    </AppShell>
  );
}
