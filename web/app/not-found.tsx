"use client";

import { useTranslation } from "react-i18next";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import AuthCard from "./components/AuthCard";
import Link from "./components/Link";
import { color, typeScale } from "@/theme/tokens";

/** Static export writes this as out/404.html; IIS points to it through web.config. */
export default function NotFound() {
  const { t } = useTranslation();

  return (
    <AuthCard title={t("notFoundPageTitle")}>
      <Stack spacing={3}>
        <Typography sx={{ ...typeScale.body, color: color.text.secondary }}>{t("notFoundPageDesc")}</Typography>
        <Button component={Link} href="/" variant="contained">
          {t("notFoundPageHome")}
        </Button>
      </Stack>
    </AuthCard>
  );
}
