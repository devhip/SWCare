"use client";

import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Pagination from "@mui/material/Pagination";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { formatDate, formatDateTime } from "@/lib/format";
import type { PageInfo } from "@/types/api";
import type { TicketListItem } from "@/types/ticket";
import { color, radiusPx, typeScale } from "@/theme/tokens";
import { PriorityChip, StatusChip } from "./StatusChip";

const wide = { display: { xs: "none", md: "table-cell" } } as const;

type Props = {
  items: TicketListItem[] | undefined;
  pageInfo?: PageInfo;
  loading: boolean;
  onPage?: (page: number) => void;
};

/** A ticket list; a row opens the ticket. Phones drop the less important columns. */
export default function TicketTable({ items, pageInfo, loading, onPage }: Props) {
  const { t, i18n } = useTranslation("ticket");
  const { t: tc } = useTranslation("common");
  const router = useRouter();

  return (
    <Stack spacing={2}>
      <Box sx={{ bgcolor: color.surface.card, border: `1px solid ${color.border.subtle}`, borderRadius: radiusPx.md, overflowX: "auto" }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>{t("list.no")}</TableCell>
              <TableCell>{t("list.title")}</TableCell>
              <TableCell>{t("list.status")}</TableCell>
              <TableCell sx={wide}>{t("list.priority")}</TableCell>
              <TableCell sx={wide}>{t("list.assignee")}</TableCell>
              <TableCell sx={wide}>{t("list.due")}</TableCell>
              <TableCell sx={wide}>{t("list.reportedAt")}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(items ?? []).map((item) => (
              <TableRow key={item.Id} hover onClick={() => router.push(`/Tickets/Detail/?id=${item.Id}`)} sx={{ cursor: "pointer" }}>
                <TableCell sx={{ whiteSpace: "nowrap", ...typeScale.bodySm14 }}>{item.TicketNo}</TableCell>
                <TableCell sx={{ minWidth: 200 }}>
                  <Typography sx={{ ...typeScale.bodySm14, fontWeight: 600, color: color.text.primary }}>{item.Title}</Typography>
                  <Typography sx={{ ...typeScale.caption12, color: color.text.secondary }}>
                    {[item.ProductName, item.CustomerName, `${t("list.reporter")} ${item.Reporter.DisplayName}`].filter(Boolean).join(" · ")}
                  </Typography>
                </TableCell>
                <TableCell>
                  <StatusChip status={item.Status} />
                </TableCell>
                <TableCell sx={wide}>
                  <PriorityChip priority={item.ActualPriority ?? item.RequestedPriority} />
                </TableCell>
                <TableCell sx={{ ...wide, ...typeScale.bodySm14 }}>{item.Assignee?.DisplayName ?? "-"}</TableCell>
                <TableCell sx={{ ...wide, ...typeScale.bodySm14, whiteSpace: "nowrap", color: item.IsOverdue ? color.semantic.error.text : undefined, fontWeight: item.IsOverdue ? 600 : undefined }}>
                  {formatDate(item.PlannedDueDate, i18n.language)}
                  {item.IsOverdue ? ` · ${t("list.overdue")}` : ""}
                </TableCell>
                <TableCell sx={{ ...wide, ...typeScale.bodySm14, whiteSpace: "nowrap" }}>{formatDateTime(item.ReportedAt, i18n.language)}</TableCell>
              </TableRow>
            ))}
            {!loading && items?.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} sx={{ textAlign: "center", py: 4, color: color.text.secondary }}>
                  {t("list.empty")}
                </TableCell>
              </TableRow>
            ) : null}
            {loading && !items ? (
              <TableRow>
                <TableCell colSpan={7} sx={{ textAlign: "center", py: 4, color: color.text.secondary }}>
                  {tc("loading")}
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </Box>
      {pageInfo && onPage ? (
        <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 1 }}>
          <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>{t("list.total", { count: pageInfo.TotalRecords })}</Typography>
          {pageInfo.TotalPages > 1 ? <Pagination count={pageInfo.TotalPages} page={pageInfo.PageNumber} onChange={(_, page) => onPage(page)} /> : null}
        </Stack>
      ) : null}
    </Stack>
  );
}
