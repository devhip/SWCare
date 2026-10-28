"use client";

import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import { useTranslation } from "react-i18next";
import { useLangStore } from "@/stores/lang.store";
import { useAuthStore } from "@/stores/auth.store";
import { setLanguageApi } from "@/lib/auth.api";
import { color, radiusPx, typeScale } from "@/theme/tokens";
import type { AppLang } from "@/lib/i18n";

/** TH / EN. When logged in, the choice is also saved to the account so it follows the user (Q49). */
export default function LanguageSwitch() {
  const { t } = useTranslation("common");
  const lang = useLangStore((s) => s.lang);
  const setLang = useLangStore((s) => s.setLang);
  const token = useAuthStore((s) => s.token);

  const choose = (code: AppLang) => {
    if (code === lang) return;
    setLang(code);
    if (token) {
      void setLanguageApi(code)
        .then((res) => useAuthStore.getState().setUser(res.Result))
        .catch(() => undefined);
    }
  };

  return (
    <Stack direction="row" spacing={0.5} sx={{ bgcolor: color.surface.sunken, borderRadius: radiusPx.md, p: 0.5 }}>
      {(["th", "en"] as AppLang[]).map((code) => {
        const selected = lang === code;
        return (
          <Button
            key={code}
            type="button"
            variant={selected ? "contained" : "text"}
            onClick={() => choose(code)}
            aria-pressed={selected}
            aria-label={code === "th" ? t("thai") : t("english")}
            sx={{
              minWidth: 48,
              minHeight: 40,
              px: 2,
              ...typeScale.caption12,
              fontWeight: selected ? 600 : 400,
              color: selected ? color.text.onBrand : color.text.secondary,
            }}
          >
            {code === "th" ? "TH" : "EN"}
          </Button>
        );
      })}
    </Stack>
  );
}
