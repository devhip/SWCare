"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AppShell from "@/app/components/AppShell";
import type { AppRole } from "@/lib/roles";
import { useTicketListQuery } from "@/queries/ticket.query";
import type { TicketScope } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";
import TicketTable from "./TicketTable";

/** A page that is one list: รออนุมัติ, รอจัดคิว. */
export default function ScopePage({ scope, roles, titleKey, hintKey }: { scope: TicketScope; roles: AppRole[]; titleKey: string; hintKey: string }) {
  const { t } = useTranslation("navigation");
  const { t: tt } = useTranslation("ticket");

  return (
    <AppShell roles={roles}>
      <Stack spacing={3}>
        <Stack spacing={0.5}>
          <Typography component="h1" sx={{ ...typeScale.h2_24, color: color.text.primary }}>
            {t(titleKey)}
          </Typography>
          <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>{tt(hintKey)}</Typography>
        </Stack>
        <ScopeList scope={scope} />
      </Stack>
    </AppShell>
  );
}

function ScopeList({ scope }: { scope: TicketScope }) {
  const [page, setPage] = useState(1);
  const list = useTicketListQuery({ Scope: scope, PageNumber: page });
  return <TicketTable items={list.data?.Result} pageInfo={list.data?.PageInfo} loading={list.isLoading} onPage={setPage} />;
}
