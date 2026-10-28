import type { MeResult } from "@/types/api";

/** Same names as AppRole in the API (plan 3). */
export type AppRole = "Admin" | "DepartmentHead" | "SupportHead" | "Programmer" | "ProgrammerLead";

export function hasRole(user: MeResult | null | undefined, role: AppRole): boolean {
  if (!user) return false;
  switch (role) {
    case "Admin":
      return user.IsAdmin;
    case "DepartmentHead":
      return user.IsDepartmentHead;
    case "SupportHead":
      return user.IsSupportHead;
    case "Programmer":
      return user.IsProgrammer;
    case "ProgrammerLead":
      return user.IsProgrammerLead;
  }
}

/** No roles = every logged-in user. */
export function hasAnyRole(user: MeResult | null | undefined, roles?: readonly AppRole[]): boolean {
  return !roles?.length || roles.some((role) => hasRole(user, role));
}
