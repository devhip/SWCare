/** Users see Thai time (Q47); the API sends UTC. */
const TIME_ZONE = "Asia/Bangkok";

/** Gregorian years in Thai too, like the ticket numbers (SW-2026-…) and the old sheet. */
function locale(lang: string) {
  return lang === "en" ? "en-GB" : "th-TH-u-ca-gregory";
}

export function formatDateTime(iso: string | null | undefined, lang: string): string {
  if (!iso) return "-";
  return new Intl.DateTimeFormat(locale(lang), {
    timeZone: TIME_ZONE,
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(iso));
}

/** DateOnly from the API ("2026-11-03"): no time zone shift. */
export function formatDate(date: string | null | undefined, lang: string): string {
  if (!date) return "-";
  const [year, month, day] = date.split("-").map(Number);
  return new Intl.DateTimeFormat(locale(lang), { day: "2-digit", month: "short", year: "numeric", timeZone: "UTC" }).format(
    new Date(Date.UTC(year, month - 1, day))
  );
}
