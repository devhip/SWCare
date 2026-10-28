import { apiGet, apiPostForm, apiPut, http } from "./http";
import type { SaveTicket, TicketDetail } from "@/types/ticket";

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
