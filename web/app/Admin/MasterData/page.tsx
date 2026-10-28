"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import TextField from "@mui/material/TextField";
import AddRounded from "@mui/icons-material/AddRounded";
import AppShell from "@/app/components/AppShell";
import { saveDeviceModelApi, saveProductApi, updateSettingsApi } from "@/lib/admin.api";
import { useAdminDeviceModelsQuery, useAdminProductsQuery, useAdminRefresh, useAdminSettingsQuery } from "@/queries/admin.query";
import { useProgrammersQuery } from "@/queries/ticket.query";
import type { DeviceModel, Product, Settings } from "@/types/ticket";
import { color, radiusPx } from "@/theme/tokens";
import { DataTable, FormDialog, PageTitle } from "../_components/AdminUi";

type MasterTab = "products" | "deviceModels" | "settings";

function ActiveName({ name, isActive }: { name: string; isActive: boolean }) {
  const { t } = useTranslation("admin");
  return (
    <Stack direction="row" sx={{ gap: 1, alignItems: "center", flexWrap: "wrap" }}>
      <span>{name}</span>
      {!isActive ? <Chip size="small" color="error" variant="outlined" label={t("users.disabled")} /> : null}
    </Stack>
  );
}

/** Products and device models share name / order / active; products add the main programmer. */
function ItemDialog({ kind, item, nextOrder, onClose }: {
  kind: "products" | "deviceModels";
  item: Product | DeviceModel | null;
  nextOrder: number;
  onClose: () => void;
}) {
  const { t } = useTranslation("admin");
  const refresh = useAdminRefresh();
  const programmers = useProgrammersQuery(kind === "products");
  const [name, setName] = useState(item?.Name ?? "");
  const [ownerUserId, setOwnerUserId] = useState<number | "">(item && "OwnerUserId" in item ? (item.OwnerUserId ?? "") : "");
  const [sortOrder, setSortOrder] = useState(String(item?.SortOrder ?? nextOrder));
  const [isActive, setIsActive] = useState(item?.IsActive ?? true);
  const [busy, setBusy] = useState(false);
  const order = Number(sortOrder);
  const orderOk = sortOrder !== "" && Number.isInteger(order) && order >= 0;

  const save = async () => {
    setBusy(true);
    try {
      const body = { Name: name.trim(), SortOrder: order, IsActive: isActive };
      if (kind === "products") await saveProductApi(item?.Id ?? null, { ...body, OwnerUserId: ownerUserId === "" ? null : ownerUserId });
      else await saveDeviceModelApi(item?.Id ?? null, body);
      toast.success(t("saved"));
      refresh();
      onClose();
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setBusy(false);
    }
  };

  const title = item ? t("master.edit") : kind === "products" ? t("master.addProduct") : t("master.addDeviceModel");
  return (
    <FormDialog title={title} busy={busy} canSave={!!name.trim() && orderOk} onClose={onClose} onSave={() => void save()}>
      <TextField label={t("master.name")} value={name} onChange={(e) => setName(e.target.value)} slotProps={{ htmlInput: { maxLength: 100 } }} />
      {kind === "products" ? (
        <TextField
          select
          label={t("master.owner")}
          value={ownerUserId}
          onChange={(e) => setOwnerUserId(e.target.value === "" ? "" : Number(e.target.value))}
          helperText={t("master.ownerHint")}
          slotProps={{ select: { displayEmpty: true }, inputLabel: { shrink: true } }}
        >
          <MenuItem value="">{t("master.noOwner")}</MenuItem>
          {(programmers.data ?? []).map((user) => (
            <MenuItem key={user.Id} value={user.Id}>
              {user.DisplayName}
            </MenuItem>
          ))}
        </TextField>
      ) : null}
      <TextField
        label={t("master.sortOrder")}
        type="number"
        value={sortOrder}
        onChange={(e) => setSortOrder(e.target.value)}
        error={!orderOk}
        slotProps={{ htmlInput: { min: 0, step: 1 } }}
      />
      <FormControlLabel control={<Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label={t("master.isActive")} />
    </FormDialog>
  );
}

function ItemList({ kind }: { kind: "products" | "deviceModels" }) {
  const { t } = useTranslation("admin");
  const products = useAdminProductsQuery();
  const deviceModels = useAdminDeviceModelsQuery();
  const [editing, setEditing] = useState<Product | DeviceModel | "new" | null>(null);
  const rows: (Product | DeviceModel)[] | undefined = kind === "products" ? products.data : deviceModels.data;
  const nextOrder = Math.max(0, ...(rows ?? []).map((row) => row.SortOrder)) + 1;

  const columns = [
    { label: t("master.name"), render: (row: Product | DeviceModel) => <ActiveName name={row.Name} isActive={row.IsActive} /> },
    ...(kind === "products"
      ? [{ label: t("master.owner"), render: (row: Product | DeviceModel) => ("OwnerName" in row && row.OwnerName) || t("none") }]
      : []),
    { label: t("master.sortOrder"), render: (row: Product | DeviceModel) => row.SortOrder },
  ];

  return (
    <Stack spacing={2}>
      <Box>
        <Button variant="contained" startIcon={<AddRounded />} onClick={() => setEditing("new")}>
          {kind === "products" ? t("master.addProduct") : t("master.addDeviceModel")}
        </Button>
      </Box>
      <DataTable rows={rows} rowKey={(row) => row.Id} onRow={setEditing} columns={columns} />
      {editing ? <ItemDialog kind={kind} item={editing === "new" ? null : editing} nextOrder={nextOrder} onClose={() => setEditing(null)} /> : null}
    </Stack>
  );
}

const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;

function SettingsForm({ settings }: { settings: Settings }) {
  const { t } = useTranslation("admin");
  const refresh = useAdminRefresh();
  const [days, setDays] = useState(String(settings.AutoCloseWorkingDays));
  const [time, setTime] = useState(settings.MorningReminderTime);
  const [template, setTemplate] = useState(settings.TestedDetailsTemplate);
  const [busy, setBusy] = useState(false);
  const dayCount = Number(days);
  const daysOk = Number.isInteger(dayCount) && dayCount >= 1 && dayCount <= 30;
  const timeOk = TIME.test(time);

  const save = async () => {
    setBusy(true);
    try {
      await updateSettingsApi({ AutoCloseWorkingDays: dayCount, MorningReminderTime: time, TestedDetailsTemplate: template });
      toast.success(t("saved"));
      refresh();
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setBusy(false);
    }
  };

  return (
    <Stack spacing={2} sx={{ maxWidth: 640, p: 3, bgcolor: color.surface.card, border: `1px solid ${color.border.subtle}`, borderRadius: radiusPx.md }}>
      <TextField label={t("master.autoCloseDays")} type="number" value={days} onChange={(e) => setDays(e.target.value)} error={!daysOk} slotProps={{ htmlInput: { min: 1, max: 30 } }} />
      <TextField label={t("master.reminderTime")} type="time" value={time} onChange={(e) => setTime(e.target.value)} error={!timeOk} slotProps={{ inputLabel: { shrink: true } }} />
      <TextField label={t("master.testedTemplate")} value={template} onChange={(e) => setTemplate(e.target.value)} multiline minRows={5} slotProps={{ htmlInput: { maxLength: 4000 } }} />
      <Box>
        <Button variant="contained" onClick={() => void save()} disabled={busy || !daysOk || !timeOk || !template.trim()}>
          {t("master.saveSettings")}
        </Button>
      </Box>
    </Stack>
  );
}

function SettingsTab() {
  const settings = useAdminSettingsQuery();
  // Keyed by the saved values so the form resets to what the server holds after a save.
  return settings.data ? <SettingsForm key={JSON.stringify(settings.data)} settings={settings.data} /> : null;
}

function MasterData() {
  const { t } = useTranslation("admin");
  const [tab, setTab] = useState<MasterTab>("products");
  return (
    <Stack spacing={3}>
      <PageTitle title={t("master.title")} />
      <Tabs value={tab} onChange={(_, value: MasterTab) => setTab(value)} variant="scrollable" allowScrollButtonsMobile>
        <Tab value="products" label={t("master.products")} />
        <Tab value="deviceModels" label={t("master.deviceModels")} />
        <Tab value="settings" label={t("master.settings")} />
      </Tabs>
      {tab === "settings" ? <SettingsTab /> : <ItemList key={tab} kind={tab} />}
    </Stack>
  );
}

export default function AdminMasterDataPage() {
  return (
    <AppShell roles={["Admin"]}>
      <MasterData />
    </AppShell>
  );
}
