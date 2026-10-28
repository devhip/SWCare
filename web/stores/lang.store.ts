import { create } from "zustand";
import { persist } from "zustand/middleware";
import i18n, { type AppLang, isAppLang } from "@/lib/i18n";

type LangState = {
  lang: AppLang;
  setLang: (lang: AppLang) => void;
};

export const useLangStore = create<LangState>()(
  persist(
    (set) => ({
      lang: "th",
      setLang: (lang) => {
        void i18n.changeLanguage(lang);
        if (typeof document !== "undefined") document.documentElement.lang = lang;
        set({ lang });
      },
    }),
    {
      name: "swcare-lang",
      skipHydration: true,
      partialize: (s) => ({ lang: s.lang }),
    }
  )
);

export function applyStoredLang() {
  const stored = useLangStore.getState().lang;
  const lang = isAppLang(stored) ? stored : "th";
  void i18n.changeLanguage(lang);
  if (typeof document !== "undefined") document.documentElement.lang = lang;
  if (lang !== stored) useLangStore.setState({ lang });
}
