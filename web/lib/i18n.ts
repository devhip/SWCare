import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import { I18N_NAMESPACES } from "./i18n/namespaces";
import { i18nResources } from "./i18n/resources";
import "@/lib/yupLocale";

export const LANGS = ["th", "en"] as const;
export type AppLang = (typeof LANGS)[number];

export function isAppLang(value: string | null | undefined): value is AppLang {
  return value === "th" || value === "en";
}

void i18n.use(initReactI18next).init({
  lng: "th",
  fallbackLng: "th",
  defaultNS: "common",
  ns: [...I18N_NAMESPACES],
  interpolation: { escapeValue: false },
  resources: i18nResources,
});

export default i18n;
