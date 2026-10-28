import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import { loadRuntimeConfig } from "./runtimeConfig";
import { notifyApiFailureIfNeeded, toApiClientError } from "./apiErrorToast";
import { useAuthStore } from "@/stores/auth.store";
import type { ApiResponse, LoginResult } from "@/types/api";

export { ApiClientError } from "./apiErrorToast";

export const http = axios.create({
  headers: { "Content-Type": "application/json" },
});

type RetryableConfig = InternalAxiosRequestConfig & { _retried?: boolean };

const AUTH_CALL = /api\/Auth\/(Login|RefreshToken|Logout)/;

http.interceptors.request.use(async (config) => {
  const { apiUrl } = await loadRuntimeConfig();
  config.baseURL = apiUrl.replace(/\/$/, "");
  const token = useAuthStore.getState().token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  if (typeof FormData !== "undefined" && config.data instanceof FormData) {
    delete config.headers["Content-Type"];
  }
  return config;
});

let refreshing: Promise<string | null> | null = null;

/**
 * New access token from the refresh token (each refresh token works once).
 * Another tab may have rotated it already, so read the saved session first.
 */
async function refreshAccessToken(failedToken: string | undefined): Promise<string | null> {
  await useAuthStore.persist.rehydrate();
  const { token, refreshToken } = useAuthStore.getState();
  if (token && token !== failedToken) return token;
  if (!refreshToken) return null;
  try {
    const { apiUrl } = await loadRuntimeConfig();
    const res = await axios.post<ApiResponse<LoginResult>>(`${apiUrl.replace(/\/$/, "")}/api/Auth/RefreshToken`, {
      RefreshToken: refreshToken,
    });
    if (!res.data.Success) return null;
    useAuthStore.getState().setSession(res.data.Result);
    return res.data.Result.AccessToken;
  } catch {
    return null;
  }
}

http.interceptors.response.use(
  (res) => res,
  async (err: AxiosError<ApiResponse<unknown>>) => {
    const original = err.config as RetryableConfig | undefined;
    const status = err.response?.status;
    const payload = {
      message: err.response?.data?.Message || err.message || undefined,
      code: err.response?.data?.Code,
    };

    if (status === 401 && original && !original._retried && !AUTH_CALL.test(original.url ?? "")) {
      original._retried = true;
      const failedToken = String(original.headers.Authorization ?? "").replace(/^Bearer /, "") || undefined;
      refreshing ??= refreshAccessToken(failedToken).finally(() => {
        refreshing = null;
      });
      const newToken = await refreshing;
      if (newToken) return http(original);
      useAuthStore.getState().logout();
      return Promise.reject(toApiClientError({ ...payload, code: "SESSION_EXPIRED" }, "session-expired"));
    }

    return Promise.reject(toApiClientError(payload));
  }
);

function afterEnvelope<T>(data: ApiResponse<T>): ApiResponse<T> {
  notifyApiFailureIfNeeded(data);
  return data;
}

export async function apiPostForm<T>(path: string, form: FormData): Promise<ApiResponse<T>> {
  const res = await http.post<ApiResponse<T>>(path.replace(/^\//, ""), form);
  return afterEnvelope(res.data);
}

export async function apiPost<T>(path: string, body?: unknown): Promise<ApiResponse<T>> {
  const res = await http.post<ApiResponse<T>>(path.replace(/^\//, ""), body ?? {});
  return afterEnvelope(res.data);
}

export async function apiGet<T>(path: string, params?: Record<string, string | number | boolean | undefined | null>): Promise<ApiResponse<T>> {
  const res = await http.get<ApiResponse<T>>(path.replace(/^\//, ""), { params });
  return afterEnvelope(res.data);
}

export async function apiDelete<T>(path: string): Promise<ApiResponse<T>> {
  const res = await http.delete<ApiResponse<T>>(path.replace(/^\//, ""));
  return afterEnvelope(res.data);
}

export async function apiPut<T>(path: string, body?: unknown): Promise<ApiResponse<T>> {
  const res = await http.put<ApiResponse<T>>(path.replace(/^\//, ""), body ?? {});
  return afterEnvelope(res.data);
}
