import { z } from "zod";

export const settingsFormSchema = z.object({
  organizationName: z.string().trim().min(2, "Required").max(200),
  organizationContactEmail: z.string().trim().min(1, "Required").email("Enter a valid email address"),
  notifyOnNewVerifiedReport: z.boolean(),
  notifyOnCriticalPriority: z.boolean(),
  defaultVerificationSlaHours: z.coerce.number().int().min(1).max(8760),
  duplicateDetectionWindowHours: z.coerce.number().int().min(1).max(8760),
  reporterDataRetentionMonths: z.coerce.number().int().min(1).max(600),
  auditLogRetentionMonths: z.coerce.number().int().min(1).max(600),
  whatsAppPlaceholderNote: z.string().trim().max(1000),
});
export type SettingsFormInput = z.input<typeof settingsFormSchema>;
export type SettingsFormValues = z.output<typeof settingsFormSchema>;
