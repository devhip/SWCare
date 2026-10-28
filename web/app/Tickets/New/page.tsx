"use client";

import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AppShell from "@/app/components/AppShell";
import { createTicketApi } from "@/lib/ticket.api";
import { useTicketFormDataQuery } from "@/queries/ticket.query";
import { color, typeScale } from "@/theme/tokens";
import TicketForm from "../_components/TicketForm";

function NewTicket() {
  const { t } = useTranslation("ticket");
  const { t: tc } = useTranslation("common");
  const router = useRouter();
  const formData = useTicketFormDataQuery();

  if (!formData.data) {
    return <Typography sx={{ color: color.text.secondary }}>{tc("loading")}</Typography>;
  }

  return (
    <TicketForm
      products={formData.data.products}
      deviceModels={formData.data.deviceModels}
      testedTemplate={formData.data.settings.TestedDetailsTemplate}
      submitLabel={t("submit")}
      submittingLabel={t("submitting")}
      withFiles
      onSubmit={async (values, files) => {
        try {
          const res = await createTicketApi(values, files);
          toast.success(t("created", { no: res.Result.TicketNo }));
          router.replace(`/Tickets/Detail/?id=${res.Result.Id}`);
        } catch {
          // The error toast is shown by lib/http.
        }
      }}
    />
  );
}

export default function NewTicketPage() {
  const { t } = useTranslation("ticket");
  return (
    <AppShell>
      <Stack spacing={3}>
        <Typography component="h1" sx={{ ...typeScale.h2_24, color: color.text.primary }}>
          {t("newTitle")}
        </Typography>
        <NewTicket />
      </Stack>
    </AppShell>
  );
}
