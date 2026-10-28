import { apiGet, apiPost, apiPut, http } from "./http";
import type { ApiListResponse, DepartmentKind } from "@/types/api";
import type { DeviceModel, Product, Settings } from "@/types/ticket";

export type AdminUser = {
  Id: number;
  Username: string;
  DisplayName: string;
  Email: string | null;
  DepartmentId: number;
  DepartmentName: string;
  IsDepartmentHead: boolean;
  IsAdmin: boolean;
  IsActive: boolean;
  MustChangePassword: boolean;
  Language: string;
  LastLoginAt: string | null;
  CreatedAt: string;
  UpdatedAt: string | null;
};

export type Department = {
  Id: number;
  Name: string;
  Kind: DepartmentKind;
  HeadUserId: number | null;
  HeadUserName: string | null;
  MemberCount: number;
  IsActive: boolean;
};

export type UserQuery = { Search?: string; DepartmentId?: number; IsActive?: boolean; PageNumber?: number; PageLimit?: number };

export async function usersApi(query: UserQuery): Promise<ApiListResponse<AdminUser>> {
  const res = await http.get<ApiListResponse<AdminUser>>("api/Users", { params: { PageLimit: 50, ...query } });
  return res.data;
}

export function createUserApi(body: { Username: string; DisplayName: string; Email: string | null; DepartmentId: number; IsAdmin: boolean; Password: string }) {
  return apiPost<AdminUser>("api/Users", body);
}

export function updateUserApi(id: number, body: { DisplayName: string; Email: string | null; DepartmentId: number; IsAdmin: boolean; IsActive: boolean }) {
  return apiPut<AdminUser>(`api/Users/${id}`, body);
}

export function resetPasswordApi(id: number, newPassword: string) {
  return apiPost<boolean>(`api/Users/${id}/ResetPassword`, { NewPassword: newPassword });
}

export function departmentsApi(includeInactive = true) {
  return apiGet<Department[]>("api/Departments", { includeInactive });
}

export function createDepartmentApi(name: string) {
  return apiPost<Department>("api/Departments", { Name: name });
}

export function updateDepartmentApi(id: number, body: { Name: string; HeadUserId: number | null; IsActive: boolean }) {
  return apiPut<Department>(`api/Departments/${id}`, body);
}

export function adminProductsApi() {
  return apiGet<Product[]>("api/Products", { includeInactive: true });
}

export function saveProductApi(id: number | null, body: { Name: string; OwnerUserId: number | null; SortOrder: number; IsActive: boolean }) {
  return id ? apiPut<Product>(`api/Products/${id}`, body) : apiPost<Product>("api/Products", body);
}

export function adminDeviceModelsApi() {
  return apiGet<DeviceModel[]>("api/DeviceModels", { includeInactive: true });
}

export function saveDeviceModelApi(id: number | null, body: { Name: string; SortOrder: number; IsActive: boolean }) {
  return id ? apiPut<DeviceModel>(`api/DeviceModels/${id}`, body) : apiPost<DeviceModel>("api/DeviceModels", body);
}

export function updateSettingsApi(body: Settings) {
  return apiPut<Settings>("api/Settings", body);
}
