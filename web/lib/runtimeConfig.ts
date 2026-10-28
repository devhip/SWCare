type RuntimeConfig = {
  apiUrl: string;
};

// public/config.json is read at run time, so one build works on any server (static export).
const FALLBACK: RuntimeConfig = { apiUrl: "http://localhost:5300" };

let cached: RuntimeConfig | null = null;
let pending: Promise<RuntimeConfig> | null = null;

export async function loadRuntimeConfig() {
  if (cached) return cached;
  if (pending) return pending;
  pending = (async () => {
    try {
      const res = await fetch("/config.json", { cache: "no-store" });
      const json = (await res.json()) as Partial<RuntimeConfig>;
      cached = { apiUrl: json.apiUrl || FALLBACK.apiUrl };
    } catch {
      cached = FALLBACK;
    }
    return cached;
  })();
  return pending;
}

export function getRuntimeConfig() {
  return cached ?? FALLBACK;
}
