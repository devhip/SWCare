"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useTranslation } from "react-i18next";
import { toast } from "react-toastify";
import Button from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import AuthCard from "@/app/components/AuthCard";
import PasswordField from "@/app/components/PasswordField";
import { loginApi } from "@/lib/auth.api";
import { isAppLang } from "@/lib/i18n";
import { homeRoute } from "@/lib/menu";
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
    if (token) router.replace(mustChangePassword ? "/ChangePassword/" : homeRoute(useAuthStore.getState().user));
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
        <Divider sx={{ ...typeScale.caption, color: color.text.secondary }}>{t("or")}</Divider>
        {/* Google login (plan 8, D15) is not wired yet: needs the OAuth Client ID and the HTTPS domain. */}
        <Button variant="outlined" size="large" startIcon={<GoogleLogo />} onClick={() => toast.info(t("googleSoon"))}>
          {t("loginWithGoogle")}
        </Button>
        <Typography sx={{ ...typeScale.caption, color: color.text.secondary, textAlign: "center" }}>{t("forgotPasswordHint")}</Typography>
      </Stack>
    </AuthCard>
  );
}

/** The four-colour "G" (Google brand guidelines keep its colours as they are). */
function GoogleLogo() {
  return (
    <svg width="18" height="18" viewBox="0 0 48 48" aria-hidden="true">
      <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z" />
      <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z" />
      <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z" />
      <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z" />
    </svg>
  );
}
