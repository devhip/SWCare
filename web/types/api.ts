/** Envelope of every API response: { Success, Message, Result, Code }. */
export type ApiResponse<T> = {
  Success: boolean;
  Message: string;
  Result: T;
  Code?: string;
};

export type PageInfo = {
  PageNumber: number;
  PageLimit: number;
  TotalPages: number;
  TotalRecords: number;
};

export type ApiListResponse<T> = ApiResponse<T[]> & {
  PageInfo?: PageInfo;
};

export type DepartmentKind = "General" | "Support" | "Software";

/** The logged-in user (GET api/Auth/Me). Roles follow from the department (plan 3). */
export type MeResult = {
  UserId: number;
  Username: string;
  DisplayName: string;
  Email: string | null;
  DepartmentId: number;
  DepartmentName: string;
  DepartmentKind: DepartmentKind;
  IsAdmin: boolean;
  IsDepartmentHead: boolean;
  IsSupportHead: boolean;
  IsProgrammer: boolean;
  IsProgrammerLead: boolean;
  /** ผู้บริหาร: Dashboard + every ticket, read-only (D10) */
  IsExecutive: boolean;
  MustChangePassword: boolean;
  Language: string;
};

export type LoginResult = {
  User: MeResult;
  AccessToken: string;
  AccessTokenExpiresAt: string;
  RefreshToken: string;
};
