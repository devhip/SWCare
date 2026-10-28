import type { AppRole } from "./roles";

export type MenuEntry = {
  route: string;
  /** Key in locales/<lang>/navigation.json */
  labelKey: string;
  icon: "myTasks";
  roles?: AppRole[];
};

/** Side menu. Add an entry when its page exists (plan 13.1: tickets day 2–3, Kanban and Admin day 4). */
export const MENU: MenuEntry[] = [{ route: "/MyTasks/", labelKey: "myTasks", icon: "myTasks" }];

export const HOME_ROUTE = "/MyTasks/";
