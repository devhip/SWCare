"use client";

import ScopePage from "@/app/Tickets/_components/ScopePage";

/** รออนุมัติ: the department head's queue (Q2). */
export default function ApprovalsPage() {
  return <ScopePage scope="ToApprove" roles={["DepartmentHead"]} titleKey="approvals" hintKey="page.approvalsHint" />;
}
