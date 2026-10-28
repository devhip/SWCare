"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import { useTranslation } from "react-i18next";
import { useAuthStore } from "@/stores/auth.store";
import { hasAnyRole, type AppRole } from "@/lib/roles";
import { HOME_ROUTE } from "@/lib/menu";
import { color, typeScale } from "@/theme/tokens";
import Link from "./Link";

const CHANGE_PASSWORD_ROUTE = "/ChangePassword/";

function Centered({ children }: { children: React.ReactNode }) {
  return (
    <Box sx={{ minHeight: "40vh", display: "flex", flexDirection: "column", gap: 2, alignItems: "center", justifyContent: "center", textAlign: "center", px: 2 }}>
      {children}
    </Box>
  );
}

/**
 * Login required. Sends a user with a first / reset password to change it first (Q41).
 * `roles` hides the page from users without any of them; the API checks the same roles again.
 */
export default function RoleGuard({ roles, children }: { roles?: AppRole[]; children: React.ReactNode }) {
  const token = useAuthStore((s) => s.token);
  const user = useAuthStore((s) => s.user);
  const hasHydrated = useAuthStore((s) => s.hasHydrated);
  const router = useRouter();
  const pathname = usePathname();
  const { t } = useTranslation("common");

  const mustChangePassword = !!user?.MustChangePassword && pathname !== CHANGE_PASSWORD_ROUTE;

  useEffect(() => {
    if (!hasHydrated) return;
    if (!token) router.replace("/Login/");
    else if (mustChangePassword) router.replace(CHANGE_PASSWORD_ROUTE);
  }, [hasHydrated, token, mustChangePassword, router]);

  if (!hasHydrated || !token || mustChangePassword) {
    return (
      <Centered>
        <Typography sx={{ ...typeScale.body16, color: color.text.secondary }}>
          {hasHydrated && !token ? t("redirectingLogin") : t("loading")}
        </Typography>
      </Centered>
    );
  }

  if (!hasAnyRole(user, roles)) {
    return (
      <Centered>
        <Typography sx={{ ...typeScale.h3_20, color: color.text.primary }}>{t("accessDeniedTitle")}</Typography>
        <Typography sx={{ ...typeScale.body16, color: color.text.secondary }}>{t("accessDeniedDesc")}</Typography>
        <Button component={Link} href={HOME_ROUTE} variant="contained">
          {t("backHome")}
        </Button>
      </Centered>
    );
  }

  return <>{children}</>;
}
