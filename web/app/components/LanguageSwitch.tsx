"use client";

import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import { useTranslation } from "react-i18next";
import { useLangStore } from "@/stores/lang.store";
import { useAuthStore } from "@/stores/auth.store";
import { setLanguageApi } from "@/lib/auth.api";
import { color, hiphr, typeScale } from "@/theme/tokens";
import type { AppLang } from "@/lib/i18n";

/**
 * TH / EN. When logged in, the choice is also saved to the account so it follows the user (Q49).
 * onHeader = white pill on the orange header (like the HIPHR header buttons).
 */
export default function LanguageSwitch({ onHeader = false }: { onHeader?: boolean }) {
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
    <Stack
      direction="row"
      spacing={0.5}
      sx={{ bgcolor: onHeader ? "rgba(255, 255, 255, .22)" : hiphr.orange100, borderRadius: "999px", p: 0.75 }}
    >
      {(["th", "en"] as AppLang[]).map((code) => {
        const selected = lang === code;
        return (
          <Button
            key={code}
            type="button"
            variant="text"
            onClick={() => choose(code)}
            aria-pressed={selected}
            aria-label={code === "th" ? t("thai") : t("english")}
            sx={{
              minWidth: 40,
              minHeight: 30,
              px: 2.5,
              borderRadius: "999px",
              ...typeScale.label,
              fontWeight: selected ? 700 : 500,
              bgcolor: selected ? "#fff" : "transparent",
              color: selected ? hiphr.orange600 : onHeader ? "#fff" : color.text.secondary,
              boxShadow: selected ? "0 2px 8px rgba(43, 26, 14, .1)" : "none",
              "&:hover": { bgcolor: selected ? "#fff" : onHeader ? "rgba(255, 255, 255, .18)" : hiphr.orange50 },
            }}
          >
            {code === "th" ? "TH" : "EN"}
          </Button>
        );
      })}
    </Stack>
  );
}
