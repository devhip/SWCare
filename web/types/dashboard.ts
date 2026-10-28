import type { TicketStatus } from "./ticket";

/** GET api/Dashboard (plan 9, D11). "Now" = current state, "InRange" = what happened between From and To. */
export type Dashboard = {
  From: string;
  To: string;
  AllDepartments: boolean;
  DepartmentId: number | null;
  Kpi: DashboardKpi;
  OpenByStatus: { Status: TicketStatus; Count: number }[];
  Months: { Month: string; New: number; Closed: number }[];
  ByProduct: Breakdown[];
  ByType: Breakdown[];
  ByDepartment: Breakdown[];
  ByCustomer: Breakdown[];
  Team: TeamMember[];
  Stages: { Stage: DashboardStage; AvgDays: number | null; Count: number }[];
  Declines: { Department: string; Count: number; Recent: { TicketId: number; TicketNo: string; Title: string; Reason: string | null; DeclinedAt: string }[] }[];
};

export type DashboardKpi = {
  OpenNow: number;
  OverdueNow: number;
  WaitingApprovalNow: number;
  WaitingTriageNow: number;
  NewInRange: number;
  ClosedInRange: number;
  DeclinedInRange: number;
  CancelledInRange: number;
  OnTimeCount: number;
  OnTimeBase: number;
  OnTimePercent: number | null;
  AvgDaysToClose: number | null;
};

/** Key = id or enum name; Label = name to show; Open = still open now. */
export type Breakdown = { Key: string; Label: string; Count: number; Open: number };

export type TeamMember = {
  UserId: number;
  Name: string;
  OpenNow: number;
  InProgressNow: number;
  OverdueNow: number;
  ResolvedInRange: number;
  AvgWorkDays: number | null;
  Reschedules: number;
};

export type DashboardStage = "Approval" | "Triage" | "Queue" | "Work" | "Close";

export type DashboardParams = { From?: string; To?: string; DepartmentId?: number; ProductId?: number };
