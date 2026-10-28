"use client";

import { Suspense, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AppShell from "@/app/components/AppShell";
import { updateTicketApi } from "@/lib/ticket.api";
import { ticketKey, useTicketFormDataQuery, useTicketQuery } from "@/queries/ticket.query";
import { color, typeScale } from "@/theme/tokens";
import TicketForm from "../_components/TicketForm";

function EditTicket() {
  const { t } = useTranslation("ticket");
  const { t: tc } = useTranslation("common");
  const router = useRouter();
  const queryClient = useQueryClient();
  const id = Number(useSearchParams().get("id")) || null;
  const ticket = useTicketQuery(id);
  const formData = useTicketFormDataQuery();

  // Editing ends when the department head approves (Q33).
  const cannotEdit = !!ticket.data && !ticket.data.AllowedActions.includes("Edit");
  useEffect(() => {
    if (cannotEdit) router.replace(`/Tickets/Detail/?id=${id}`);
  }, [cannotEdit, id, router]);

  if (ticket.isError) return <Typography sx={{ color: color.text.secondary }}>{t("notFound")}</Typography>;
  if (!ticket.data || !formData.data || cannotEdit) return <Typography sx={{ color: color.text.secondary }}>{tc("loading")}</Typography>;

  return (
    <Stack spacing={2}>
      <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>
        {ticket.data.TicketNo} · {ticket.data.Title}
      </Typography>
      <TicketForm
        products={formData.data.products}
        deviceModels={formData.data.deviceModels}
        testedTemplate={formData.data.settings.TestedDetailsTemplate}
        initial={ticket.data}
        submitLabel={t("saveChanges")}
        submittingLabel={t("submitting")}
        withFiles={false}
        onSubmit={async (values) => {
          try {
            const res = await updateTicketApi(ticket.data.Id, values);
            queryClient.setQueryData(ticketKey(res.Result.Id), res.Result);
            toast.success(t("saved"));
            router.replace(`/Tickets/Detail/?id=${res.Result.Id}`);
          } catch {
            // The error toast is shown by lib/http.
          }
        }}
      />
    </Stack>
  );
}

export default function EditTicketPage() {
  const { t } = useTranslation("ticket");
  return (
    <AppShell>
      <Stack spacing={3}>
        <Typography component="h1" sx={{ ...typeScale.h2_24, color: color.text.primary }}>
          {t("editTitle")}
        </Typography>
        <Suspense>
          <EditTicket />
        </Suspense>
      </Stack>
    </AppShell>
  );
}
