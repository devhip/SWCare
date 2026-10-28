export type TicketType = "Bug" | "FeatureRequest" | "Testing" | "DataService";
export const TICKET_TYPES: TicketType[] = ["Bug", "FeatureRequest", "Testing", "DataService"];

export type TicketPriority = "Immediate" | "Urgent" | "Normal";
export const TICKET_PRIORITIES: TicketPriority[] = ["Immediate", "Urgent", "Normal"];

export type TicketStatus =
  | "PendingApproval"
  | "Rejected"
  | "PendingTriage"
  | "WaitingInfo"
  | "Queued"
  | "InProgress"
  | "Resolved"
  | "Closed"
  | "Cancelled"
  | "Declined";

/** Same names as TicketAction in the API (BLC/TicketWorkflow.cs). */
export type TicketAction =
  | "Edit"
  | "Resubmit"
  | "Approve"
  | "Reject"
  | "Cancel"
  | "Triage"
  | "RequestInfo"
  | "Decline"
  | "Start"
  | "Pause"
  | "Resolve"
  | "Close"
  | "SendBack"
  | "Reschedule"
  | "Reassign"
  | "Comment"
  | "AddAttachment";

export type TicketActivityType =
  | "Created"
  | "Edited"
  | "Resubmitted"
  | "Approved"
  | "AutoApproved"
  | "Rejected"
  | "Triaged"
  | "ClassificationChanged"
  | "InfoRequested"
  | "InfoProvided"
  | "Started"
  | "Paused"
  | "Resolved"
  | "SentBack"
  | "Closed"
  | "AutoClosed"
  | "Cancelled"
  | "Declined"
  | "Rescheduled"
  | "Reassigned"
  | "WatcherAdded"
  | "WatcherRemoved"
  | "LinkAdded"
  | "Imported";

export type UserBrief = { Id: number; DisplayName: string };
export type IdName = { Id: number; Name: string };

export type Attachment = {
  Id: number;
  FileName: string;
  ContentType: string;
  SizeBytes: number;
  UploadedBy: UserBrief;
  CreatedAt: string;
};

export type Comment = {
  Id: number;
  Author: UserBrief;
  Body: string;
  CreatedAt: string;
  Attachments: Attachment[];
};

export type Activity = {
  Id: number;
  Type: TicketActivityType;
  FromStatus: TicketStatus | null;
  ToStatus: TicketStatus | null;
  Actor: UserBrief | null;
  Note: string | null;
  /** JSON, e.g. {"Changes":[{"Field":"Title","Old":"..","New":".."}]} for Edited. */
  Data: string | null;
  CreatedAt: string;
};

export type TicketDetail = {
  Id: number;
  TicketNo: string;
  LegacyRef: string | null;
  Title: string;
  Type: TicketType;
  Product: IdName;
  Version: string | null;
  CustomerName: string | null;
  Description: string;
  TestedDetails: string | null;
  RequestedPriority: TicketPriority;
  ActualPriority: TicketPriority | null;
  Status: TicketStatus;
  Reporter: UserBrief;
  Requester: UserBrief | null;
  Department: IdName;
  Approver: UserBrief | null;
  ApprovedAt: string | null;
  Assignee: UserBrief | null;
  PlannedStartDate: string | null;
  PlannedDueDate: string | null;
  ReportedAt: string;
  StartedAt: string | null;
  ResolvedAt: string | null;
  ClosedAt: string | null;
  Resolution: string | null;
  ReturnCount: number;
  RescheduleCount: number;
  NeedsLegacyReview: boolean;
  LegacyReporterName: string | null;
  LegacyRequesterName: string | null;
  LegacyApproverName: string | null;
  LegacyAssigneeName: string | null;
  DeviceModels: IdName[];
  Watchers: UserBrief[];
  Attachments: Attachment[];
  Comments: Comment[];
  Activities: Activity[];
  AllowedActions: TicketAction[];
};

/** Body of create / update (SaveTicketReq). */
export type SaveTicket = {
  Title: string;
  Type: TicketType;
  ProductId: number;
  Version: string | null;
  CustomerName: string | null;
  Description: string;
  TestedDetails: string | null;
  RequestedPriority: TicketPriority;
  RequesterId: number | null;
  DeviceModelIds: number[];
};

export type Product = { Id: number; Name: string; OwnerUserId: number | null; OwnerName: string | null; SortOrder: number; IsActive: boolean };
export type DeviceModel = { Id: number; Name: string; SortOrder: number; IsActive: boolean };
export type Settings = { AutoCloseWorkingDays: number; MorningReminderTime: string; TestedDetailsTemplate: string };
export type UserLookup = { Id: number; DisplayName: string; DepartmentId: number; DepartmentName: string };
