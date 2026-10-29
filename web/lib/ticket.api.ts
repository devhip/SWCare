import { apiDelete, apiGet, apiPost, apiPostForm, apiPut, http } from "./http";
import type { ApiListResponse } from "@/types/api";
import type { Board, SaveTicket, TicketAction, TicketCounts, TicketDetail, TicketListItem, TicketListParams } from "@/types/ticket";

/** Statuses go as repeated query keys (Statuses=A&Statuses=B), as ASP.NET binds lists. */
export async function listTicketsApi(params: TicketListParams): Promise<ApiListResponse<TicketListItem>> {
  const query = new URLSearchParams();
  if (params.Scope) query.set("Scope", params.Scope);
  params.Statuses?.forEach((status) => query.append("Statuses", status));
  if (params.Search) query.set("Search", params.Search);
  if (params.ProductId) query.set("ProductId", String(params.ProductId));
  query.set("PageNumber", String(params.PageNumber ?? 1));
  query.set("PageLimit", String(params.PageLimit ?? 20));
  const res = await http.get<ApiListResponse<TicketListItem>>(`api/Tickets?${query.toString()}`);
  return res.data;
}

export function boardApi() {
  return apiGet<Board>("api/Tickets/Board");
}

/** The lead's new order of one programmer's queue (Q30). */
export function reorderQueueApi(assigneeId: number, ticketIds: number[]) {
  return apiPost<boolean>("api/Tickets/Board/Reorder", { AssigneeId: assigneeId, TicketIds: ticketIds });
}

export function ticketCountsApi() {
  return apiGet<TicketCounts>("api/Tickets/Counts");
}

/** One endpoint per status change: POST api/Tickets/{id}/{action} (plan 4.3). */
export function ticketActionApi(id: number, action: TicketAction, body?: unknown) {
  return apiPost<TicketDetail>(`api/Tickets/${id}/${action}`, body ?? {});
}

export function addWatcherApi(id: number, userId: number) {
  return apiPost<TicketDetail>(`api/Tickets/${id}/Watchers`, { UserId: userId });
}

export function removeWatcherApi(id: number, userId: number) {
  return apiDelete<TicketDetail>(`api/Tickets/${id}/Watchers/${userId}`);
}

export function followApi(id: number) {
  return apiPost<TicketDetail>(`api/Tickets/${id}/Follow`);
}

export function unfollowApi(id: number) {
  return apiDelete<TicketDetail>(`api/Tickets/${id}/Follow`);
}

/** Multipart: the ticket as JSON plus its files, in one request. */
export function createTicketApi(ticket: SaveTicket, files: File[]) {
  const form = new FormData();
  form.append("Ticket", JSON.stringify(ticket));
  files.forEach((file) => form.append("Files", file));
  return apiPostForm<TicketDetail>("api/Tickets", form);
}

export function ticketApi(id: number) {
  return apiGet<TicketDetail>(`api/Tickets/${id}`);
}

export function updateTicketApi(id: number, ticket: SaveTicket) {
  return apiPut<TicketDetail>(`api/Tickets/${id}`, ticket);
}

export function addTicketFilesApi(id: number, files: File[]) {
  const form = new FormData();
  files.forEach((file) => form.append("Files", file));
  return apiPostForm<TicketDetail>(`api/Tickets/${id}/Attachments`, form);
}

export function addCommentApi(id: number, body: string, files: File[]) {
  const form = new FormData();
  form.append("Body", body);
  files.forEach((file) => form.append("Files", file));
  return apiPostForm<TicketDetail>(`api/Tickets/${id}/Comments`, form);
}

export function customerNamesApi(search?: string) {
  return apiGet<string[]>("api/Tickets/CustomerNames", { search });
}

/** Files need the access token, so they come through axios and open from a blob URL. */
export async function downloadAttachment(ticketId: number, attachmentId: number, fileName: string) {
  const res = await http.get<Blob>(`api/Tickets/${ticketId}/Attachments/${attachmentId}`, { responseType: "blob" });
  const url = URL.createObjectURL(res.data);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
