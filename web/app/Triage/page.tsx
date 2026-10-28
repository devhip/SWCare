"use client";

import ScopePage from "@/app/Tickets/_components/ScopePage";

/** รอจัดคิว: the programmer lead's queue (Q21, Q22). */
export default function TriagePage() {
  return <ScopePage scope="ToTriage" roles={["ProgrammerLead"]} titleKey="triage" hintKey="page.triageHint" />;
}
