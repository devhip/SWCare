import { toast } from "react-toastify";
import i18n from "@/lib/i18n";
import type { ApiResponse } from "@/types/api";

export class ApiClientError extends Error {
  readonly toasted = true;
  readonly code?: string;

  constructor(message: string, code?: string) {
    super(message);
    this.name = "ApiClientError";
    this.code = code;
  }
}

export type ApiErrorPayload = {
  message?: string | null;
  code?: string | null;
};

export function resolveApiErrorMessage(payload: ApiErrorPayload): string {
  const code = payload.code?.trim();
  if (code && i18n.exists(code, { ns: "errors" })) {
    return i18n.t(code, { ns: "errors" });
  }
  const message = payload.message?.trim();
  if (message) return message;
  return i18n.t("requestFailed", { ns: "errors" });
}

const openToastIds = new Set<string>();

export function showApiErrorToast(payload: ApiErrorPayload, toastId?: string): string {
  const text = resolveApiErrorMessage(payload);
  if (toastId && (openToastIds.has(toastId) || toast.isActive(toastId))) return text;
  if (toastId) openToastIds.add(toastId);
  toast.error(text, {
    toastId,
    position: "top-right",
    autoClose: 5000,
    closeOnClick: true,
    pauseOnHover: true,
    className: "sw-api-error-toast",
    onClose: () => {
      if (toastId) openToastIds.delete(toastId);
    },
  });
  return text;
}

export function notifyApiFailureIfNeeded(data: ApiResponse<unknown> | null | undefined): void {
  if (!data || data.Success) return;
  showApiErrorToast({ message: data.Message, code: data.Code });
}

export function toApiClientError(payload: ApiErrorPayload, toastId?: string): ApiClientError {
  const text = showApiErrorToast(payload, toastId);
  return new ApiClientError(text, payload.code ?? undefined);
}
