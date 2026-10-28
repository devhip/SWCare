"use client";

import { useTranslation } from "react-i18next";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import { card, color, typeScale } from "@/theme/tokens";

export function PageTitle({ title, hint, action }: { title: string; hint?: string; action?: React.ReactNode }) {
  return (
    <Stack direction={{ xs: "column", md: "row" }} spacing={2} sx={{ justifyContent: "space-between", alignItems: { md: "flex-start" } }}>
      <Stack spacing={0.5}>
        <Typography component="h1" sx={{ ...typeScale.pageTitle, color: color.text.primary }}>
          {title}
        </Typography>
        {hint ? <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{hint}</Typography> : null}
      </Stack>
      {action}
    </Stack>
  );
}

/** A plain table; rows open an editor. */
export function DataTable<T>({ columns, rows, rowKey, onRow }: {
  columns: { label: string; render: (row: T) => React.ReactNode; wide?: boolean }[];
  rows: T[] | undefined;
  rowKey: (row: T) => string | number;
  onRow?: (row: T) => void;
}) {
  const { t } = useTranslation("common");
  return (
    <Box sx={{ ...card, overflowX: "auto" }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            {columns.map((column, index) => (
              <TableCell key={index} sx={column.wide ? { display: { xs: "none", md: "table-cell" } } : undefined}>
                {column.label}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {(rows ?? []).map((row) => (
            <TableRow key={rowKey(row)} hover={!!onRow} onClick={onRow ? () => onRow(row) : undefined} sx={{ cursor: onRow ? "pointer" : undefined }}>
              {columns.map((column, index) => (
                <TableCell key={index} sx={{ ...typeScale.small, ...(column.wide ? { display: { xs: "none", md: "table-cell" } } : {}) }}>
                  {column.render(row)}
                </TableCell>
              ))}
            </TableRow>
          ))}
          {!rows ? (
            <TableRow>
              <TableCell colSpan={columns.length} sx={{ textAlign: "center", py: 4, color: color.text.secondary }}>
                {t("loading")}
              </TableCell>
            </TableRow>
          ) : null}
        </TableBody>
      </Table>
    </Box>
  );
}

/** The theme's sm breakpoint is 0 (Easy Next), so dialogs set their own width. */
export function FormDialog({ title, busy, canSave, onClose, onSave, children }: {
  title: string;
  busy: boolean;
  canSave: boolean;
  onClose: () => void;
  onSave: () => void;
  children: React.ReactNode;
}) {
  const { t } = useTranslation("admin");
  const { t: tc } = useTranslation("common");
  return (
    <Dialog open onClose={busy ? undefined : onClose} maxWidth={false} slotProps={{ paper: { sx: { width: "100%", maxWidth: 560 } } }}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={4} sx={{ pt: 1 }}>
          {children}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button variant="outlined" onClick={onClose} disabled={busy}>
          {tc("cancel")}
        </Button>
        <Button variant="contained" onClick={onSave} disabled={busy || !canSave}>
          {t("save")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
