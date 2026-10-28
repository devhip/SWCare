"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import AppShell from "@/app/components/AppShell";
import TicketTable from "@/app/Tickets/_components/TicketTable";
import { hasAnyRole, type AppRole } from "@/lib/roles";
import { useTicketCountsQuery, useTicketListQuery } from "@/queries/ticket.query";
import { useAuthStore } from "@/stores/auth.store";
import { OPEN_STATUSES, type TicketScope } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";

type MyScope = Exclude<TicketScope, "All" | "ToTriage">;

/** Plan 9: ที่ต้องทำ / ที่ต้องอนุมัติ / ที่รอฉันปิด / ที่ฉันแจ้ง / ที่ร้องขอ-ติดตาม. */
const TABS: { scope: MyScope; roles?: AppRole[] }[] = [
  { scope: "ToDo", roles: ["Programmer"] },
  { scope: "ToApprove", roles: ["DepartmentHead"] },
  { scope: "ToClose" },
  { scope: "Reported" },
  { scope: "Following" },
];

function TabList({ scope }: { scope: MyScope }) {
  const [page, setPage] = useState(1);
  // ที่ฉันแจ้ง and ที่ร้องขอ/ติดตาม show open work; the other lists are open by definition.
  const statuses = scope === "Reported" || scope === "Following" ? OPEN_STATUSES : undefined;
  const list = useTicketListQuery({ Scope: scope, Statuses: statuses, PageNumber: page });
  return <TicketTable items={list.data?.Result} pageInfo={list.data?.PageInfo} loading={list.isLoading} onPage={setPage} />;
}

function MyTasks() {
  const { t } = useTranslation("ticket");
  const { t: tc } = useTranslation("common");
  const user = useAuthStore((s) => s.user);
  const tabs = TABS.filter((tab) => hasAnyRole(user, tab.roles));
  const counts = useTicketCountsQuery();
  const [chosen, setChosen] = useState<MyScope | null>(null);

  // Open on the first tab that has work waiting; ที่ฉันแจ้ง when there is none.
  const firstWithWork = counts.data ? (tabs.find((tab) => counts.data[tab.scope] > 0)?.scope ?? "Reported") : null;
  const scope = chosen ?? firstWithWork;

  if (!scope) return <Typography sx={{ color: color.text.secondary }}>{tc("loading")}</Typography>;

  return (
    <Stack spacing={2}>
      <Tabs value={scope} onChange={(_, value: MyScope) => setChosen(value)} variant="scrollable" allowScrollButtonsMobile>
        {tabs.map((tab) => {
          const count = counts.data?.[tab.scope] ?? 0;
          return (
            <Tab
              key={tab.scope}
              value={tab.scope}
              label={
                <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                  <span>{t(`tab.${tab.scope}`)}</span>
                  {count > 0 ? <Chip label={count} size="small" color="primary" sx={{ height: 20 }} /> : null}
                </Stack>
              }
            />
          );
        })}
      </Tabs>
      <TabList key={scope} scope={scope} />
    </Stack>
  );
}

export default function MyTasksPage() {
  const { t } = useTranslation("navigation");
  return (
    <AppShell>
      <Stack spacing={5}>
        <Typography component="h1" sx={{ ...typeScale.pageTitle, color: color.text.primary }}>
          {t("myTasks")}
        </Typography>
        <MyTasks />
      </Stack>
    </AppShell>
  );
}
