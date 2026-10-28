"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useTranslation } from "react-i18next";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import AuthCard from "@/app/components/AuthCard";
import PasswordField from "@/app/components/PasswordField";
import { loginApi } from "@/lib/auth.api";
import { isAppLang } from "@/lib/i18n";
import { HOME_ROUTE } from "@/lib/menu";
import { useAuthStore } from "@/stores/auth.store";
import { useLangStore } from "@/stores/lang.store";
import { color, typeScale } from "@/theme/tokens";

const schema = yup.object({
  username: yup.string().trim().required(),
  password: yup.string().required(),
});
type LoginForm = yup.InferType<typeof schema>;

export default function LoginPage() {
  const { t } = useTranslation("auth");
  const { t: tv } = useTranslation("validation");
  const router = useRouter();
  const token = useAuthStore((s) => s.token);
  const mustChangePassword = useAuthStore((s) => !!s.user?.MustChangePassword);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: yupResolver(schema), defaultValues: { username: "", password: "" } });

  // Already logged in (or just logged in): go on.
  useEffect(() => {
    if (token) router.replace(mustChangePassword ? "/ChangePassword/" : HOME_ROUTE);
  }, [token, mustChangePassword, router]);

  const onSubmit = handleSubmit(async (form) => {
    try {
      const res = await loginApi({ Username: form.username, Password: form.password });
      // The account remembers its language (Q49).
      if (isAppLang(res.Result.User.Language)) useLangStore.getState().setLang(res.Result.User.Language);
      useAuthStore.getState().setSession(res.Result);
    } catch {
      // The error toast is shown by lib/http.
    }
  });

  return (
    <AuthCard title={t("loginTitle")} subtitle={t("loginSubtitle")}>
      <Stack component="form" spacing={2.5} onSubmit={onSubmit} noValidate>
        <TextField
          label={t("username")}
          autoComplete="username"
          autoFocus
          fullWidth
          {...register("username")}
          error={!!errors.username}
          helperText={errors.username ? tv("required") : undefined}
        />
        <PasswordField
          label={t("password")}
          autoComplete="current-password"
          {...register("password")}
          error={!!errors.password}
          helperText={errors.password ? tv("required") : undefined}
        />
        <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
          {isSubmitting ? t("loggingIn") : t("login")}
        </Button>
        <Typography sx={{ ...typeScale.caption, color: color.text.secondary, textAlign: "center" }}>{t("forgotPasswordHint")}</Typography>
      </Stack>
    </AuthCard>
  );
}
