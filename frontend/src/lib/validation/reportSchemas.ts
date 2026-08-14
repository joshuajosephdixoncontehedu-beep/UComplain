import { z } from "zod";
import { IncidentPriority, VerificationDecisionAction } from "@/types/enums";

export const updateReportSchema = z.object({
  categoryId: z.string().min(1, "Choose a category"),
  priority: z.enum(IncidentPriority),
  locationDescription: z.string().trim().min(3, "Enter a location"),
  description: z.string().trim().min(10, "Description must be at least 10 characters"),
});
export type UpdateReportFormValues = z.infer<typeof updateReportSchema>;

export const addNoteSchema = z.object({
  content: z.string().trim().min(3, "Note must be at least 3 characters"),
});
export type AddNoteFormValues = z.infer<typeof addNoteSchema>;

export const assignReportSchema = z.object({
  adminUserId: z.string().min(1, "Choose an administrator"),
});
export type AssignReportFormValues = z.infer<typeof assignReportSchema>;

export const changeStatusSchema = z.object({
  notes: z.string().trim().max(1000).optional(),
});
export type ChangeStatusFormValues = z.infer<typeof changeStatusSchema>;

// Approve is a straightforward confirmation; every other decision changes the
// reporter-facing outcome (rejection, duplicate, escalation, clarification) and must
// carry a human-readable reason for the audit trail.
export function reasonRequiredFor(action: VerificationDecisionAction): boolean {
  return action !== VerificationDecisionAction.Approve;
}

export const verificationDecisionSchema = z
  .object({
    action: z.enum(VerificationDecisionAction),
    reason: z.string().trim().max(1000).optional(),
  })
  .refine((value) => !reasonRequiredFor(value.action) || (value.reason?.length ?? 0) >= 5, {
    message: "Provide a reason of at least 5 characters for this decision",
    path: ["reason"],
  });
export type VerificationDecisionFormValues = z.infer<typeof verificationDecisionSchema>;
