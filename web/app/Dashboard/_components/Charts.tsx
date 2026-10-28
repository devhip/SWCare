"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { card, color, typeScale } from "@/theme/tokens";

/**
 * Chart colours, validated with the dataviz validator on white and on the cream page (all checks pass):
 * one hue for single-series bars, categorical slots 1–2 for the two monthly series.
 */
export const VIZ = {
  bar: "#F06400",
  series1: "#2a78d6",
  series2: "#eb6834",
  grid: "#F3E6DA",
  track: "#FBF1E8",
} as const;

const number = new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 });
export const formatNumber = (value: number) => number.format(value);

/** White card with a title and a one-line hint, like the HIPHR cards. */
export function Panel({ title, hint, action, children }: { title: string; hint?: string; action?: React.ReactNode; children: React.ReactNode }) {
  return (
    <Stack spacing={4} sx={{ ...card, p: { xs: 4, md: 5 }, minWidth: 0 }}>
      <Stack direction="row" spacing={2} sx={{ alignItems: "flex-start", justifyContent: "space-between" }}>
        <Stack spacing={0.5} sx={{ minWidth: 0 }}>
          <Typography component="h2" sx={{ ...typeScale.sectionTitle, color: color.text.primary }}>
            {title}
          </Typography>
          {hint ? <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{hint}</Typography> : null}
        </Stack>
        {action}
      </Stack>
      {children}
    </Stack>
  );
}

/** Stat tile: label · value · one supporting line. Big numbers keep proportional digits. */
export function StatTile({ label, value, sub, tone }: { label: string; value: string; sub?: string; tone?: "warning" }) {
  return (
    <Stack spacing={1} sx={{ ...card, p: { xs: 4, md: 5 }, minWidth: 0 }}>
      <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
        {tone === "warning" ? <Box sx={{ width: 8, height: 8, borderRadius: "50%", bgcolor: color.semantic.error.base, flexShrink: 0 }} /> : null}
        <Typography sx={{ ...typeScale.label, color: color.text.secondary }}>{label}</Typography>
      </Stack>
      <Typography sx={{ ...typeScale.kpi, fontVariantNumeric: "normal", color: color.text.primary }}>{value}</Typography>
      {sub ? <Typography sx={{ ...typeScale.caption, color: color.text.secondary }}>{sub}</Typography> : null}
    </Stack>
  );
}

export type BarItem = { key: string; label: string; value: number; display?: string; tooltip?: string };

/**
 * Horizontal bars, one hue: label · bar from a common baseline (≤ 16 px, 4 px rounded data end) · value at the tip.
 * The whole row is the hover target.
 */
export function HBarList({ items, emptyText }: { items: BarItem[]; emptyText: string }) {
  if (!items.length) {
    return <Typography sx={{ ...typeScale.small, color: color.text.secondary, py: 4, textAlign: "center" }}>{emptyText}</Typography>;
  }

  const max = Math.max(...items.map((item) => item.value), 1);
  return (
    <Stack spacing={1.5}>
      {items.map((item) => (
        <Tooltip key={item.key} title={item.tooltip ?? ""} placement="top" followCursor disableHoverListener={!item.tooltip}>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "minmax(0, 7fr) minmax(0, 9fr)", md: "minmax(0, 2fr) minmax(0, 3fr)" },
              alignItems: "center",
              gap: 3,
              py: 1,
              px: 1.5,
              mx: -1.5,
              borderRadius: "8px",
              "&:hover": { bgcolor: VIZ.track },
            }}
          >
            <Typography noWrap sx={{ ...typeScale.small, color: color.text.primary }} title={item.label}>
              {item.label}
            </Typography>
            <Stack direction="row" spacing={2} sx={{ alignItems: "center", minWidth: 0 }}>
              <Box sx={{ flex: `0 0 ${(item.value / max) * 82}%`, height: 14, bgcolor: VIZ.bar, borderRadius: "0 4px 4px 0", minWidth: item.value > 0 ? 3 : 0 }} />
              <Typography sx={{ ...typeScale.label, color: color.text.primary, fontVariantNumeric: "tabular-nums", whiteSpace: "nowrap" }}>
                {item.display ?? formatNumber(item.value)}
              </Typography>
            </Stack>
          </Box>
        </Tooltip>
      ))}
    </Stack>
  );
}

/** 0, then 4 clean steps up to at least the largest value. */
function niceTicks(max: number): number[] {
  if (max <= 4) return [0, 1, 2, 3, 4].slice(0, Math.max(max, 1) + 1);
  const rough = max / 4;
  const power = 10 ** Math.floor(Math.log10(rough));
  const step = [1, 2, 2.5, 5, 10].map((m) => m * power).find((s) => s >= rough) ?? rough;
  return Array.from({ length: Math.ceil(max / step) + 1 }, (_, i) => i * step);
}

export type MonthPoint = { key: string; label: string; first: number; second: number };

/**
 * Two series per month as paired columns (≤ 24 px, 2 px surface gap, 4 px rounded caps), one y-axis with hairline
 * gridlines, legend above, tooltip per month, and a table view.
 */
export function MonthColumns({ points, firstLabel, secondLabel }: { points: MonthPoint[]; firstLabel: string; secondLabel: string }) {
  const { t } = useTranslation("dashboard");
  const [showTable, setShowTable] = useState(false);
  const ticks = niceTicks(Math.max(...points.map((p) => Math.max(p.first, p.second)), 1));
  const top = ticks[ticks.length - 1] || 1;
  const height = 200;
  // narrower columns for long ranges, so every month fits without scrolling
  const barWidth = points.length > 18 ? 5 : points.length > 12 ? 8 : 18;

  const legend = (
    <Stack direction="row" spacing={4} sx={{ flexWrap: "wrap" }}>
      {[
        [firstLabel, VIZ.series1],
        [secondLabel, VIZ.series2],
      ].map(([label, fill]) => (
        <Stack key={label} direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
          <Box sx={{ width: 10, height: 10, borderRadius: "3px", bgcolor: fill }} />
          <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{label}</Typography>
        </Stack>
      ))}
    </Stack>
  );

  return (
    <Stack spacing={3}>
      <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 2 }}>
        {legend}
        <Button size="small" variant="text" onClick={() => setShowTable((v) => !v)}>
          {showTable ? t("months.hideTable") : t("months.showTable")}
        </Button>
      </Stack>

      {showTable ? (
        <Box sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>{t("months.month")}</TableCell>
                <TableCell align="right">{firstLabel}</TableCell>
                <TableCell align="right">{secondLabel}</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {points.map((p) => (
                <TableRow key={p.key}>
                  <TableCell>{p.label}</TableCell>
                  <TableCell align="right">{p.first}</TableCell>
                  <TableCell align="right">{p.second}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      ) : (
        <Box sx={{ display: "grid", gridTemplateColumns: "auto minmax(0, 1fr)", columnGap: 2 }}>
          {/* y-axis */}
          <Box sx={{ position: "relative", height, width: 28 }}>
            {ticks.map((tick) => (
              <Typography
                key={tick}
                sx={{ ...typeScale.caption, color: color.text.tertiary, position: "absolute", right: 0, bottom: `calc(${(tick / top) * 100}% - 9px)`, fontVariantNumeric: "tabular-nums" }}
              >
                {formatNumber(tick)}
              </Typography>
            ))}
          </Box>
          {/* plot */}
          <Box sx={{ position: "relative", height }}>
            {ticks.map((tick) => (
              <Box key={tick} sx={{ position: "absolute", left: 0, right: 0, bottom: `${(tick / top) * 100}%`, borderTop: `1px solid ${VIZ.grid}` }} />
            ))}
            <Box sx={{ position: "absolute", inset: 0, display: "flex", alignItems: "flex-end" }}>
              {points.map((p) => (
                <Tooltip
                  key={p.key}
                  placement="top"
                  title={
                    <Stack spacing={0.5}>
                      <b>{p.label}</b>
                      <span>
                        {firstLabel}: {p.first}
                      </span>
                      <span>
                        {secondLabel}: {p.second}
                      </span>
                    </Stack>
                  }
                >
                  <Box
                    sx={{
                      flex: 1,
                      minWidth: 0,
                      height: "100%",
                      display: "flex",
                      alignItems: "flex-end",
                      justifyContent: "center",
                      gap: "2px",
                      borderRadius: "6px 6px 0 0",
                      "&:hover": { bgcolor: "rgba(240, 100, 0, .05)" },
                    }}
                  >
                    {[
                      [p.first, VIZ.series1],
                      [p.second, VIZ.series2],
                    ].map(([value, fill], i) => (
                      <Box key={i} sx={{ width: barWidth, height: `${((value as number) / top) * 100}%`, minHeight: (value as number) > 0 ? 3 : 0, bgcolor: fill as string, borderRadius: "4px 4px 0 0" }} />
                    ))}
                  </Box>
                </Tooltip>
              ))}
            </Box>
          </Box>
          {/* x-axis labels */}
          <Box />
          <Box sx={{ display: "flex", pt: 1.5, overflow: "hidden" }}>
            {points.map((p, index) => (
              <Typography
                key={p.key}
                noWrap
                sx={{
                  ...typeScale.caption,
                  flex: 1,
                  minWidth: 0,
                  textAlign: "center",
                  color: color.text.secondary,
                  // long ranges: label every other month
                  visibility: points.length > 12 && index % 2 === 1 ? "hidden" : "visible",
                }}
              >
                {p.label}
              </Typography>
            ))}
          </Box>
        </Box>
      )}
    </Stack>
  );
}
