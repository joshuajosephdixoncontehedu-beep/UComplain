import { differenceInHours, parseISO } from "date-fns";
import type { BadgeTone } from "./statusStyles";

export interface VerificationAge {
  hoursElapsed: number;
  label: string;
  tone: BadgeTone;
}

/** SLA age relative to the report's category SLA — drives the queue's age indicator. */
export function verificationAge(createdAt: string, slaHours: number): VerificationAge {
  const hoursElapsed = differenceInHours(new Date(), parseISO(createdAt));
  const ratio = slaHours > 0 ? hoursElapsed / slaHours : 0;

  const tone: BadgeTone = ratio >= 1 ? "red" : ratio >= 0.75 ? "amber" : "slate";
  const label = ratio >= 1 ? "Overdue" : ratio >= 0.75 ? "Due soon" : "Within SLA";

  return { hoursElapsed, label, tone };
}
