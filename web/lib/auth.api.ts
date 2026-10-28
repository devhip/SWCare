import { apiGet, apiPost, apiPut } from "./http";
import type { LoginResult, MeResult } from "@/types/api";

export function loginApi(body: { Username: string; Password: string }) {
  return apiPost<LoginResult>("api/Auth/Login", body);
}

export function changePasswordApi(body: { CurrentPassword: string; NewPassword: string }) {
  return apiPost<LoginResult>("api/Auth/ChangePassword", body);
}

export function logoutApi(refreshToken: string) {
  return apiPost<boolean>("api/Auth/Logout", { RefreshToken: refreshToken });
}

export function meApi() {
  return apiGet<MeResult>("api/Auth/Me");
}

export function setLanguageApi(language: string) {
  return apiPut<MeResult>("api/Auth/Me/Language", { Language: language });
}
