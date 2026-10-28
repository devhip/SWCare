import type { AppRole } from "./roles";

export type MenuEntry = {
  route: string;
  /** Key in locales/<lang>/navigation.json */
  labelKey: string;
  icon: "myTasks" | "newTicket";
  roles?: AppRole[];
};

/** Side menu. Add an entry when its page exists (plan 13.1: lists day 3, Kanban and Admin day 4). */
export const MENU: MenuEntry[] = [
  { route: "/MyTasks/", labelKey: "myTasks", icon: "myTasks" },
  { route: "/Tickets/New/", labelKey: "newTicket", icon: "newTicket" },
];

export const HOME_ROUTE = "/MyTasks/";
