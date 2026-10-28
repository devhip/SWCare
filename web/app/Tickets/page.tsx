"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import InputAdornment from "@mui/material/InputAdornment";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import SearchRounded from "@mui/icons-material/SearchRounded";
import AppShell from "@/app/components/AppShell";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { useTicketFormDataQuery, useTicketListQuery } from "@/queries/ticket.query";
import { ALL_STATUSES, OPEN_STATUSES, type TicketStatus } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";
import TicketTable from "./_components/TicketTable";

/** "open" = every status that is not over yet. */
type StatusFilter = "all" | "open" | TicketStatus;

function AllTickets() {
  const { t } = useTranslation("ticket");
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search.trim());
  const [status, setStatus] = useState<StatusFilter>("open");
  const [productId, setProductId] = useState<number | "">("");
  const [page, setPage] = useState(1);
  const formData = useTicketFormDataQuery();

  const statuses = status === "all" ? undefined : status === "open" ? OPEN_STATUSES : [status];
  const list = useTicketListQuery({ Search: debouncedSearch, Statuses: statuses, ProductId: productId || undefined, PageNumber: page });

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <TextField
          placeholder={t("list.search")}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          fullWidth
          slotProps={{ input: { startAdornment: <InputAdornment position="start"><SearchRounded /></InputAdornment> } }}
        />
        <TextField
          select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as StatusFilter);
            setPage(1);
          }}
          sx={{ minWidth: { md: 220 } }}
        >
          <MenuItem value="open">{t("status.open")}</MenuItem>
          <MenuItem value="all">{t("list.allStatuses")}</MenuItem>
          {ALL_STATUSES.map((value) => (
            <MenuItem key={value} value={value}>
              {t(`status.${value}`)}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          value={productId}
          onChange={(e) => {
            setProductId(e.target.value === "" ? "" : Number(e.target.value));
            setPage(1);
          }}
          sx={{ minWidth: { md: 240 } }}
          slotProps={{ select: { displayEmpty: true } }}
        >
          <MenuItem value="">{t("list.allProducts")}</MenuItem>
          {(formData.data?.products ?? []).map((product) => (
            <MenuItem key={product.Id} value={product.Id}>
              {product.Name}
            </MenuItem>
          ))}
        </TextField>
      </Stack>
      <TicketTable items={list.data?.Result} pageInfo={list.data?.PageInfo} loading={list.isLoading} onPage={setPage} />
    </Stack>
  );
}

export default function AllTicketsPage() {
  const { t } = useTranslation("navigation");
  const { t: tt } = useTranslation("ticket");
  return (
    <AppShell>
      <Stack spacing={5}>
        <Stack spacing={0.5}>
          <Typography component="h1" sx={{ ...typeScale.pageTitle, color: color.text.primary }}>
            {t("allTickets")}
          </Typography>
          <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{tt("page.allHint")}</Typography>
        </Stack>
        <AllTickets />
      </Stack>
    </AppShell>
  );
}
