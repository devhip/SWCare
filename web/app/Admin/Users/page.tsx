"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import FormControlLabel from "@mui/material/FormControlLabel";
import InputAdornment from "@mui/material/InputAdornment";
import MenuItem from "@mui/material/MenuItem";
import Pagination from "@mui/material/Pagination";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import AddRounded from "@mui/icons-material/AddRounded";
import SearchRounded from "@mui/icons-material/SearchRounded";
import AppShell from "@/app/components/AppShell";
import PasswordField from "@/app/components/PasswordField";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { createUserApi, resetPasswordApi, updateUserApi, type AdminUser, type Department } from "@/lib/admin.api";
import { formatDateTime } from "@/lib/format";
import { useAdminRefresh, useAdminUsersQuery, useDepartmentsQuery } from "@/queries/admin.query";
import { color, typeScale } from "@/theme/tokens";
import { DataTable, FormDialog, PageTitle } from "../_components/AdminUi";

const USERNAME = /^[A-Za-z0-9._-]+$/;

/** Same rule as the API: 8–64 characters and not the username. */
function passwordError(password: string, username: string): string | null {
  if (password.length < 8) return "passwordMin";
  if (password.length > 64) return "passwordMax";
  if (password.toLowerCase() === username.toLowerCase()) return "passwordSameAsUsername";
  return null;
}

function UserChips({ user }: { user: AdminUser }) {
  const { t } = useTranslation("admin");
  return (
    <Stack direction="row" sx={{ gap: 0.5, flexWrap: "wrap" }}>
      {user.IsDepartmentHead ? <Chip size="small" color="primary" variant="outlined" label={t("users.head")} /> : null}
      {user.IsAdmin ? <Chip size="small" color="secondary" variant="outlined" label={t("users.admin")} /> : null}
      {!user.IsActive ? <Chip size="small" color="error" variant="outlined" label={t("users.disabled")} /> : null}
      {user.IsActive && user.MustChangePassword ? <Chip size="small" color="warning" variant="outlined" label={t("users.mustChange")} /> : null}
    </Stack>
  );
}

function UserDialog({ user, departments, onClose }: { user: AdminUser | null; departments: Department[]; onClose: () => void }) {
  const { t, i18n } = useTranslation("admin");
  const { t: tv } = useTranslation("validation");
  const refresh = useAdminRefresh();
  const [username, setUsername] = useState(user?.Username ?? "");
  const [displayName, setDisplayName] = useState(user?.DisplayName ?? "");
  const [email, setEmail] = useState(user?.Email ?? "");
  const [departmentId, setDepartmentId] = useState<number | "">(user?.DepartmentId ?? "");
  const [isAdmin, setIsAdmin] = useState(user?.IsAdmin ?? false);
  const [isActive, setIsActive] = useState(user?.IsActive ?? true);
  const [password, setPassword] = useState("");
  const [resetting, setResetting] = useState(false);
  const [busy, setBusy] = useState(false);

  const usernameBad = !user && username !== "" && !USERNAME.test(username);
  const pwError = (!user || resetting) && password ? passwordError(password, username) : null;
  const canSave = resetting
    ? !!password && !pwError
    : !!username.trim() && !usernameBad && !!displayName.trim() && departmentId !== "" && (!!user || (!!password && !pwError));

  const save = async () => {
    setBusy(true);
    try {
      if (resetting && user) {
        await resetPasswordApi(user.Id, password);
        toast.success(t("users.passwordReset"));
      } else if (user) {
        await updateUserApi(user.Id, { DisplayName: displayName.trim(), Email: email.trim() || null, DepartmentId: Number(departmentId), IsAdmin: isAdmin, IsActive: isActive });
        toast.success(t("saved"));
      } else {
        await createUserApi({ Username: username.trim(), DisplayName: displayName.trim(), Email: email.trim() || null, DepartmentId: Number(departmentId), IsAdmin: isAdmin, Password: password });
        toast.success(t("saved"));
      }
      refresh();
      onClose();
    } catch {
      // The error toast is shown by lib/http.
    } finally {
      setBusy(false);
    }
  };

  const pwHelper = pwError ? tv(pwError, { min: 8, max: 64 }) : undefined;

  if (resetting && user) {
    return (
      <FormDialog title={`${t("users.resetPassword")} — ${user.Username}`} busy={busy} canSave={canSave} onClose={onClose} onSave={() => void save()}>
        <Typography sx={{ ...typeScale.small, color: color.text.secondary }}>{t("users.resetHint")}</Typography>
        <PasswordField label={t("users.newPassword")} value={password} onChange={(e) => setPassword(e.target.value)} error={!!pwError} helperText={pwHelper} autoComplete="new-password" />
      </FormDialog>
    );
  }

  return (
    <FormDialog title={user ? t("users.edit") : t("users.add")} busy={busy} canSave={canSave} onClose={onClose} onSave={() => void save()}>
      <TextField
        label={t("users.username")}
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        disabled={!!user}
        error={usernameBad}
        helperText={user ? undefined : t("users.usernameHint")}
        slotProps={{ htmlInput: { maxLength: 50, autoComplete: "off" } }}
      />
      <TextField label={t("users.displayName")} value={displayName} onChange={(e) => setDisplayName(e.target.value)} slotProps={{ htmlInput: { maxLength: 100 } }} />
      <TextField label={t("users.email")} type="email" value={email} onChange={(e) => setEmail(e.target.value)} slotProps={{ htmlInput: { maxLength: 200 } }} />
      <TextField select label={t("users.department")} value={departmentId} onChange={(e) => setDepartmentId(Number(e.target.value))}>
        {departments
          .filter((department) => department.IsActive || department.Id === user?.DepartmentId)
          .map((department) => (
            <MenuItem key={department.Id} value={department.Id}>
              {department.Name}
            </MenuItem>
          ))}
      </TextField>
      {!user ? (
        <PasswordField label={t("users.password")} value={password} onChange={(e) => setPassword(e.target.value)} error={!!pwError} helperText={pwHelper} autoComplete="new-password" />
      ) : null}
      <Stack direction="row" sx={{ flexWrap: "wrap", gap: 2 }}>
        <FormControlLabel control={<Switch checked={isAdmin} onChange={(e) => setIsAdmin(e.target.checked)} />} label={t("users.isAdmin")} />
        {user ? <FormControlLabel control={<Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label={t("users.isActive")} /> : null}
      </Stack>
      {user ? (
        <Stack direction="row" sx={{ justifyContent: "space-between", alignItems: "center", gap: 2, flexWrap: "wrap" }}>
          <Typography sx={{ ...typeScale.caption, color: color.text.secondary }}>
            {t("users.lastLogin")}: {formatDateTime(user.LastLoginAt, i18n.language) || t("none")}
          </Typography>
          <Button variant="outlined" onClick={() => setResetting(true)} disabled={!user.IsActive}>
            {t("users.resetPassword")}
          </Button>
        </Stack>
      ) : null}
    </FormDialog>
  );
}

function Users() {
  const { t, i18n } = useTranslation("admin");
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search.trim());
  const [departmentId, setDepartmentId] = useState<number | "">("");
  const [page, setPage] = useState(1);
  const [editing, setEditing] = useState<AdminUser | "new" | null>(null);
  const departments = useDepartmentsQuery();
  const users = useAdminUsersQuery({ Search: debouncedSearch || undefined, DepartmentId: departmentId || undefined, PageNumber: page });
  const pageInfo = users.data?.PageInfo;

  return (
    <Stack spacing={5}>
      <PageTitle
        title={t("users.title")}
        hint={t("users.hint")}
        action={
          <Button variant="contained" startIcon={<AddRounded />} onClick={() => setEditing("new")} disabled={!departments.data} sx={{ flexShrink: 0 }}>
            {t("users.add")}
          </Button>
        }
      />
      <Stack direction={{ xs: "column", md: "row" }} spacing={4}>
        <TextField
          placeholder={t("users.search")}
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
          value={departmentId}
          onChange={(e) => {
            setDepartmentId(e.target.value === "" ? "" : Number(e.target.value));
            setPage(1);
          }}
          sx={{ minWidth: { md: 240 } }}
          slotProps={{ select: { displayEmpty: true } }}
        >
          <MenuItem value="">{t("users.allDepartments")}</MenuItem>
          {(departments.data ?? []).map((department) => (
            <MenuItem key={department.Id} value={department.Id}>
              {department.Name}
            </MenuItem>
          ))}
        </TextField>
      </Stack>
      <DataTable
        rows={users.data?.Result}
        rowKey={(user) => user.Id}
        onRow={setEditing}
        columns={[
          { label: t("users.username"), render: (user) => user.Username },
          {
            label: t("users.displayName"),
            render: (user) => (
              <Stack spacing={0.5}>
                <span>{user.DisplayName}</span>
                <UserChips user={user} />
              </Stack>
            ),
          },
          { label: t("users.department"), render: (user) => user.DepartmentName },
          { label: t("users.email"), render: (user) => user.Email || t("none"), wide: true },
          { label: t("users.lastLogin"), render: (user) => formatDateTime(user.LastLoginAt, i18n.language) || t("none"), wide: true },
        ]}
      />
      {pageInfo && pageInfo.TotalPages > 1 ? <Pagination count={pageInfo.TotalPages} page={pageInfo.PageNumber} onChange={(_, value) => setPage(value)} /> : null}
      {editing && departments.data ? (
        <UserDialog user={editing === "new" ? null : editing} departments={departments.data} onClose={() => setEditing(null)} />
      ) : null}
    </Stack>
  );
}

export default function AdminUsersPage() {
  return (
    <AppShell roles={["Admin"]}>
      <Users />
    </AppShell>
  );
}
