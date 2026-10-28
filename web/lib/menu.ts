import type { AppRole } from "./roles";
import type { TicketCounts } from "@/types/ticket";

export type MenuEntry = {
  route: string;
  /** Key in locales/<lang>/navigation.json */
  labelKey: string;
  icon: "myTasks" | "newTicket" | "approvals" | "triage" | "kanban" | "allTickets" | "adminUsers" | "adminDepartments" | "adminMasterData";
  roles?: AppRole[];
  /** Shows the number of open tickets of this list next to the label. */
  badge?: keyof TicketCounts;
  /** Entries of the admin section get a heading above the first one. */
  section?: "admin";
};

/** Side menu. Add an entry when its page exists. */
export const MENU: MenuEntry[] = [
  { route: "/MyTasks/", labelKey: "myTasks", icon: "myTasks" },
  { route: "/Tickets/New/", labelKey: "newTicket", icon: "newTicket" },
  { route: "/Approvals/", labelKey: "approvals", icon: "approvals", roles: ["DepartmentHead"], badge: "ToApprove" },
  { route: "/Triage/", labelKey: "triage", icon: "triage", roles: ["ProgrammerLead"], badge: "ToTriage" },
  { route: "/Kanban/", labelKey: "kanban", icon: "kanban", roles: ["Programmer"] },
  { route: "/Tickets/", labelKey: "allTickets", icon: "allTickets" },
  { route: "/Admin/Users/", labelKey: "adminUsers", icon: "adminUsers", roles: ["Admin"], section: "admin" },
  { route: "/Admin/Departments/", labelKey: "adminDepartments", icon: "adminDepartments", roles: ["Admin"], section: "admin" },
  { route: "/Admin/MasterData/", labelKey: "adminMasterData", icon: "adminMasterData", roles: ["Admin"], section: "admin" },
];

export const HOME_ROUTE = "/MyTasks/";
