import { apiGet } from "./http";
import type { DeviceModel, Product, Settings, UserLookup } from "@/types/ticket";

export function productsApi() {
  return apiGet<Product[]>("api/Products");
}

export function deviceModelsApi() {
  return apiGet<DeviceModel[]>("api/DeviceModels");
}

export function settingsApi() {
  return apiGet<Settings>("api/Settings");
}

export function userLookupApi(search?: string) {
  return apiGet<UserLookup[]>("api/Users/Lookup", { search });
}
