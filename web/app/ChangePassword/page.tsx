"use client";

import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import AuthCard from "@/app/components/AuthCard";
import PasswordField from "@/app/components/PasswordField";
import RoleGuard from "@/app/components/RoleGuard";
import { changePasswordApi } from "@/lib/auth.api";
import { homeRoute } from "@/lib/menu";
import { useAuthStore } from "@/stores/auth.store";

/** Same limits as PasswordRules in the API. */
const PASSWORD_MIN = 8;
const PASSWORD_MAX = 64;

function buildSchema(username: string) {
  return yup.object({
    currentPassword: yup.string().required(),
    newPassword: yup
      .string()
      .required()
      .min(PASSWORD_MIN, "passwordMin")
      .max(PASSWORD_MAX, "passwordMax")
      .notOneOf([yup.ref("currentPassword")], "passwordSameAsCurrent")
      .test("not-username", "passwordSameAsUsername", (value) => !value || value.toLowerCase() !== username.toLowerCase()),
    confirmPassword: yup
      .string()
      .required()
      .oneOf([yup.ref("newPassword")], "passwordMismatch"),
  });
}
type ChangePasswordValues = yup.InferType<ReturnType<typeof buildSchema>>;

function ChangePasswordForm() {
  const { t } = useTranslation("auth");
  const { t: tc } = useTranslation("common");
  const { t: tv } = useTranslation("validation");
  const router = useRouter();
  const user = useAuthStore((s) => s.user);
  const forced = !!user?.MustChangePassword;

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordValues>({
    resolver: yupResolver(buildSchema(user?.Username ?? "")),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  /** yup messages are i18n keys of the "validation" namespace. */
  const message = (key?: string) => (key ? tv(key, { min: PASSWORD_MIN, max: PASSWORD_MAX }) : undefined);

  const onSubmit = handleSubmit(async (form) => {
    try {
      const res = await changePasswordApi({ CurrentPassword: form.currentPassword, NewPassword: form.newPassword });
      useAuthStore.getState().setSession(res.Result);
      toast.success(t("passwordChanged"));
      router.replace(homeRoute(res.Result.User));
    } catch {
      // The error toast is shown by lib/http.
    }
  });

  return (
    <AuthCard title={t("changePasswordTitle")} subtitle={forced ? t("changePasswordForced") : t("passwordRule", { min: PASSWORD_MIN })}>
      <Stack component="form" spacing={2.5} onSubmit={onSubmit} noValidate>
        <PasswordField
          label={t("currentPassword")}
          autoComplete="current-password"
          autoFocus
          {...register("currentPassword")}
          error={!!errors.currentPassword}
          helperText={message(errors.currentPassword?.message)}
        />
        <PasswordField
          label={t("newPassword")}
          autoComplete="new-password"
          {...register("newPassword")}
          error={!!errors.newPassword}
          helperText={message(errors.newPassword?.message) ?? (forced ? t("passwordRule", { min: PASSWORD_MIN }) : undefined)}
        />
        <PasswordField
          label={t("confirmPassword")}
          autoComplete="new-password"
          {...register("confirmPassword")}
          error={!!errors.confirmPassword}
          helperText={message(errors.confirmPassword?.message)}
        />
        <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
          {isSubmitting ? t("saving") : t("saveNewPassword")}
        </Button>
        {forced ? null : (
          <Button type="button" variant="text" onClick={() => router.back()}>
            {tc("cancel")}
          </Button>
        )}
      </Stack>
    </AuthCard>
  );
}

export default function ChangePasswordPage() {
  return (
    <RoleGuard>
      <ChangePasswordForm />
    </RoleGuard>
  );
}
