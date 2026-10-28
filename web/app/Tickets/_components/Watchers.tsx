"use client";

import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import Autocomplete from "@mui/material/Autocomplete";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { userLookupApi } from "@/lib/masterData.api";
import { addWatcherApi, removeWatcherApi } from "@/lib/ticket.api";
import { TICKET_LISTS_KEY } from "@/queries/ticket.query";
import type { TicketDetail, UserLookup } from "@/types/ticket";
import { color, typeScale } from "@/theme/tokens";

/** ผู้ติดตาม: see the ticket and get its notifications (Q39). */
export default function Watchers({ ticket, onChanged }: { ticket: TicketDetail; onChanged: (ticket: TicketDetail) => void }) {
  const { t } = useTranslation("ticket");
  const queryClient = useQueryClient();
  const canManage = ticket.AllowedActions.includes("ManageWatchers");
  const [search, setSearch] = useState("");
  const debounced = useDebouncedValue(search);
  const [busy, setBusy] = useState(false);
  const users = useQuery({
    queryKey: ["userLookup", debounced],
    queryFn: async () => (await userLookupApi(debounced)).Result,
    enabled: canManage,
  });

  const change = async (call: () => Promise<{ Result: TicketDetail }>) => {
    setBusy(true);
    try {
      onChanged((await call()).Result);
      void queryClient.invalidateQueries({ queryKey: TICKET_LISTS_KEY });
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setBusy(false);
    }
  };

  const taken = new Set(ticket.Watchers.map((w) => w.Id));

  return (
    <Stack spacing={1}>
      {ticket.Watchers.length ? (
        <Stack direction="row" sx={{ flexWrap: "wrap", gap: 1 }}>
          {ticket.Watchers.map((watcher) => (
            <Chip
              key={watcher.Id}
              label={watcher.DisplayName}
              size="small"
              onDelete={canManage && !busy ? () => void change(() => removeWatcherApi(ticket.Id, watcher.Id)) : undefined}
            />
          ))}
        </Stack>
      ) : (
        <Typography sx={{ ...typeScale.bodySm14, color: color.text.secondary }}>{t("watchers.none")}</Typography>
      )}
      {canManage ? (
        <Autocomplete<UserLookup>
          size="small"
          options={(users.data ?? []).filter((u) => !taken.has(u.Id))}
          value={null}
          disabled={busy}
          onChange={(_, user) => user && void change(() => addWatcherApi(ticket.Id, user.Id))}
          onInputChange={(_, value, reason) => reason === "input" && setSearch(value)}
          getOptionLabel={(option) => option.DisplayName}
          renderOption={(props, option) => (
            <li {...props} key={option.Id}>
              {option.DisplayName} · {option.DepartmentName}
            </li>
          )}
          renderInput={(params) => <TextField {...params} label={t("watchers.add")} />}
        />
      ) : null}
    </Stack>
  );
}
