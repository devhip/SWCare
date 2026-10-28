"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import AddRounded from "@mui/icons-material/AddRounded";
import AppShell from "@/app/components/AppShell";
import { createDepartmentApi, updateDepartmentApi, type Department } from "@/lib/admin.api";
import { useAdminRefresh, useAdminUsersQuery, useDepartmentsQuery } from "@/queries/admin.query";
import { color, typeScale } from "@/theme/tokens";
import { DataTable, FormDialog, PageTitle } from "../_components/AdminUi";

function DepartmentDialog({ department, onClose }: { department: Department | null; onClose: () => void }) {
  const { t } = useTranslation("admin");
  const refresh = useAdminRefresh();
  const [name, setName] = useState(department?.Name ?? "");
  const [headUserId, setHeadUserId] = useState<number | "">(department?.HeadUserId ?? "");
  const [isActive, setIsActive] = useState(department?.IsActive ?? true);
  const [busy, setBusy] = useState(false);
  // A new department has no members yet, so the head is set after people move in.
  const members = useAdminUsersQuery({ DepartmentId: department?.Id, IsActive: true, PageLimit: 200 }, !!department);

  const save = async () => {
    setBusy(true);
    try {
      if (department) {
        await updateDepartmentApi(department.Id, { Name: name.trim(), HeadUserId: headUserId === "" ? null : headUserId, IsActive: isActive });
      } else {
        await createDepartmentApi(name.trim());
      }
      toast.success(t("saved"));
      refresh();
      onClose();
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setBusy(false);
    }
  };

  return (
    <FormDialog title={department ? t("departments.edit") : t("departments.add")} busy={busy} canSave={!!name.trim()} onClose={onClose} onSave={() => void save()}>
      <TextField label={t("departments.name")} value={name} onChange={(e) => setName(e.target.value)} slotProps={{ htmlInput: { maxLength: 100 } }} />
      {department ? (
        <>
          <TextField label={t("departments.kind")} value={t(`departments.kindName.${department.Kind}`)} disabled />
          <TextField
            select
            label={t("departments.head")}
            value={headUserId}
            onChange={(e) => setHeadUserId(e.target.value === "" ? "" : Number(e.target.value))}
            helperText={t("departments.headHint")}
            slotProps={{ select: { displayEmpty: true }, inputLabel: { shrink: true } }}
          >
            <MenuItem value="">{t("departments.noHead")}</MenuItem>
            {(members.data?.Result ?? []).map((user) => (
              <MenuItem key={user.Id} value={user.Id}>
                {user.DisplayName} ({user.Username})
              </MenuItem>
            ))}
          </TextField>
          <FormControlLabel control={<Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label={t("users.isActive")} />
        </>
      ) : null}
    </FormDialog>
  );
}

function Departments() {
  const { t } = useTranslation("admin");
  const [editing, setEditing] = useState<Department | "new" | null>(null);
  const departments = useDepartmentsQuery();

  return (
    <Stack spacing={3}>
      <PageTitle
        title={t("departments.title")}
        hint={t("departments.hint")}
        action={
          <Button variant="contained" startIcon={<AddRounded />} onClick={() => setEditing("new")} sx={{ flexShrink: 0 }}>
            {t("departments.add")}
          </Button>
        }
      />
      <DataTable
        rows={departments.data}
        rowKey={(department) => department.Id}
        onRow={setEditing}
        columns={[
          {
            label: t("departments.name"),
            render: (department) => (
              <Stack direction="row" sx={{ gap: 1, alignItems: "center", flexWrap: "wrap" }}>
                <span>{department.Name}</span>
                {!department.IsActive ? <Chip size="small" color="error" variant="outlined" label={t("users.disabled")} /> : null}
              </Stack>
            ),
          },
          { label: t("departments.kind"), render: (department) => t(`departments.kindName.${department.Kind}`) },
          {
            label: t("departments.head"),
            render: (department) =>
              department.HeadUserName ?? <span style={{ ...typeScale.bodySm14, color: color.semantic.warning.text }}>{t("departments.noHead")}</span>,
          },
          { label: t("departments.members"), render: (department) => department.MemberCount, wide: true },
        ]}
      />
      {editing ? <DepartmentDialog department={editing === "new" ? null : editing} onClose={() => setEditing(null)} /> : null}
    </Stack>
  );
}

export default function AdminDepartmentsPage() {
  return (
    <AppShell roles={["Admin"]}>
      <Departments />
    </AppShell>
  );
}
