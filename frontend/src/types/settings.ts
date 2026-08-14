export interface Settings {
  organizationName: string;
  organizationContactEmail: string;
  notifyOnNewVerifiedReport: boolean;
  notifyOnCriticalPriority: boolean;
  defaultVerificationSlaHours: number;
  duplicateDetectionWindowHours: number;
  reporterDataRetentionMonths: number;
  auditLogRetentionMonths: number;
  whatsAppIntegrationEnabled: boolean;
  whatsAppPlaceholderNote: string | null;
  updatedAt: string;
}

export type UpdateSettingsRequest = Omit<Settings, "whatsAppIntegrationEnabled" | "updatedAt">;
