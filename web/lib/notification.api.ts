import { apiGet, apiPost } from "./http";

export type NotificationType =
  | "TicketSubmitted"
  | "TicketApproved"
  | "TicketRejected"
  | "TicketTriaged"
  | "InfoRequested"
  | "InfoProvided"
  | "TicketStarted"
  | "TicketPaused"
  | "TicketRescheduled"
  | "TicketReassigned"
  | "TicketResolved"
  | "TicketSentBack"
  | "TicketClosed"
  | "TicketAutoClosed"
  | "TicketDeclined"
  | "TicketCancelled"
  | "CommentAdded"
  | "WatcherAdded"
  | "PendingApprovalReminder"
  | "AutoCloseReminder"
  | "OverdueReminder";

export type AppNotification = {
  Id: number;
  Type: NotificationType;
  TicketId: number | null;
  ActorName: string | null;
  /** JSON: TicketNo, Title and the event's values (Assignee, PlannedDueDate, NewDueDate, CloseDate, Count, RequestedPriority …). */
  Data: string | null;
  CreatedAt: string;
  IsRead: boolean;
};

export function notificationsApi(pageLimit = 20) {
  return apiGet<AppNotification[]>("api/Notifications", { PageLimit: pageLimit });
}

export function unreadCountApi() {
  return apiGet<number>("api/Notifications/UnreadCount");
}

export function markReadApi(id: number) {
  return apiPost<boolean>(`api/Notifications/${id}/Read`);
}

export function markAllReadApi() {
  return apiPost<boolean>("api/Notifications/ReadAll");
}
