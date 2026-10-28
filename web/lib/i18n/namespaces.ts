export const I18N_NAMESPACES = ["common", "auth", "navigation", "validation", "errors"] as const;
export type I18nNamespace = (typeof I18N_NAMESPACES)[number];
