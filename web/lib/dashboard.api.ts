import { apiGet, http } from "./http";
import type { Dashboard, DashboardParams } from "@/types/dashboard";

export function dashboardApi(params: DashboardParams) {
  return apiGet<Dashboard>("api/Dashboard", params);
}

/** .xlsx with the same filters (D12); the API names the file. */
export async function exportDashboardApi(params: DashboardParams) {
  const res = await http.get<Blob>("api/Dashboard/Export", { params, responseType: "blob" });
  const disposition = String(res.headers["content-disposition"] ?? "");
  const fileName = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)?.[1] ?? "SWCare-Dashboard.xlsx";
  const url = URL.createObjectURL(res.data);
  const link = document.createElement("a");
  link.href = url;
  link.download = decodeURIComponent(fileName);
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
