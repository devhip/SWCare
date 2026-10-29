type RuntimeConfig = {
  apiUrl: string;
};

// public/config.json is read at run time, so one build works on any server (static export).
// "{host}" in apiUrl = the host the page was opened with, so the dev web works from localhost and from the LAN.
const FALLBACK: RuntimeConfig = { apiUrl: "http://{host}:5300" };

function withHost(apiUrl: string) {
  return apiUrl.replace("{host}", typeof window === "undefined" ? "localhost" : window.location.hostname);
}

let cached: RuntimeConfig | null = null;
let pending: Promise<RuntimeConfig> | null = null;

export async function loadRuntimeConfig() {
  if (cached) return cached;
  if (pending) return pending;
  pending = (async () => {
    try {
      const res = await fetch("/config.json", { cache: "no-store" });
      const json = (await res.json()) as Partial<RuntimeConfig>;
      cached = { apiUrl: withHost(json.apiUrl || FALLBACK.apiUrl) };
    } catch {
      cached = { apiUrl: withHost(FALLBACK.apiUrl) };
    }
    return cached;
  })();
  return pending;
}

export function getRuntimeConfig() {
  return cached ?? { apiUrl: withHost(FALLBACK.apiUrl) };
}
