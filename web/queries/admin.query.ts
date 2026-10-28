import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import { settingsApi } from "@/lib/masterData.api";
import { adminDeviceModelsApi, adminProductsApi, departmentsApi, usersApi, type UserQuery } from "@/lib/admin.api";

export const ADMIN_KEY = ["admin"] as const;

export function useAdminUsersQuery(query: UserQuery, enabled = true) {
  return useQuery({
    queryKey: [...ADMIN_KEY, "users", query],
    queryFn: () => usersApi(query),
    placeholderData: keepPreviousData,
    enabled,
  });
}

export function useDepartmentsQuery() {
  return useQuery({
    queryKey: [...ADMIN_KEY, "departments"],
    queryFn: async () => (await departmentsApi()).Result,
  });
}

export function useAdminProductsQuery() {
  return useQuery({
    queryKey: [...ADMIN_KEY, "products"],
    queryFn: async () => (await adminProductsApi()).Result,
  });
}

export function useAdminDeviceModelsQuery() {
  return useQuery({
    queryKey: [...ADMIN_KEY, "deviceModels"],
    queryFn: async () => (await adminDeviceModelsApi()).Result,
  });
}

/** After a save: refresh the admin lists and everything built from master data (ticket form, programmer lists). */
export function useAdminRefresh() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ADMIN_KEY });
    void queryClient.invalidateQueries({ queryKey: ["ticketFormData"] });
    void queryClient.invalidateQueries({ queryKey: ["programmers"] });
  };
}

export function useAdminSettingsQuery() {
  return useQuery({
    queryKey: [...ADMIN_KEY, "settings"],
    queryFn: async () => (await settingsApi()).Result,
  });
}
