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
  | "AddAttachment"
  | "ManageWatchers"
  | "Follow"
  | "Unfollow";

/** Same names as TicketScope in the API. */
export type TicketScope = "All" | "Reported" | "Following" | "ToDo" | "ToApprove" | "ToClose" | "ToTriage";

/** Not over yet: what the งานของฉัน tabs show by default. */
export const OPEN_STATUSES: TicketStatus[] = ["PendingApproval", "Rejected", "PendingTriage", "WaitingInfo", "Queued", "InProgress", "Resolved"];
export const ALL_STATUSES: TicketStatus[] = [...OPEN_STATUSES, "Closed", "Cancelled", "Declined"];

export type TicketListItem = {
  Id: number;
  TicketNo: string;
  Title: string;
  Type: TicketType;
  ProductName: string;
  Status: TicketStatus;
  RequestedPriority: TicketPriority;
  ActualPriority: TicketPriority | null;
  Reporter: UserBrief;
  Requester: UserBrief | null;
  Assignee: UserBrief | null;
  DepartmentName: string;
  CustomerName: string | null;
  ReportedAt: string;
  PlannedDueDate: string | null;
  IsOverdue: boolean;
};

export type TicketCounts = {
  Reported: number;
  Following: number;
  ToDo: number;
  ToApprove: number;
  ToClose: number;
  ToTriage: number;
};

export type BoardCard = {
  Id: number;
  TicketNo: string;
  Title: string;
  ProductName: string;
  CustomerName: string | null;
  Status: TicketStatus;
  RequestedPriority: TicketPriority;
  ActualPriority: TicketPriority | null;
  AssigneeId: number | null;
  PlannedDueDate: string | null;
  IsOverdue: boolean;
  QueueOrder: number;
  AllowedActions: TicketAction[];
};

export type Board = { Programmers: UserBrief[]; Cards: BoardCard[] };

export type TicketListParams = {
  Scope?: TicketScope;
  Statuses?: TicketStatus[];
  Search?: string;
  ProductId?: number;
  PageNumber?: number;
  PageLimit?: number;
};

export type TriageBody = {
  ActualPriority: TicketPriority;
  AssigneeId: number;
  PlannedStartDate: string;
  PlannedDueDate: string;
  ProductId: number;
  Type: TicketType;
  DeviceModelIds: number[];
  CustomerName: string | null;
};

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
