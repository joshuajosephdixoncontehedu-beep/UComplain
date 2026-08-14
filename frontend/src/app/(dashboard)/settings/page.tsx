"use client";

import { useEffect } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Loader2, MessageCircle, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { ChartCard } from "@/components/dashboard/ChartCard";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isSuperAdmin } from "@/lib/auth/permissions";
import { getSettings, updateSettings } from "@/lib/api/settings";
import { ApiError } from "@/lib/api/client";
import { settingsFormSchema, type SettingsFormInput, type SettingsFormValues } from "@/lib/validation/settingsSchemas";
import { formatDateTime } from "@/lib/utils/format";

export default function SettingsPage() {
  const { admin } = useAuth();
  const canManage = admin ? isSuperAdmin(admin.role) : false;
  const queryClient = useQueryClient();

  const { data: settings, isLoading, isError, error } = useQuery({
    queryKey: ["settings"],
    queryFn: getSettings,
    enabled: canManage,
  });

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isDirty },
  } = useForm<SettingsFormInput, unknown, SettingsFormValues>({
    resolver: zodResolver(settingsFormSchema),
    // Booleans need a real default (not undefined) from the first render, or the
    // Switch below starts uncontrolled and then warns when `reset()` gives it a value
    // once the settings query resolves.
    defaultValues: {
      organizationName: "",
      organizationContactEmail: "",
      notifyOnNewVerifiedReport: false,
      notifyOnCriticalPriority: false,
      defaultVerificationSlaHours: 24,
      duplicateDetectionWindowHours: 24,
      reporterDataRetentionMonths: 12,
      auditLogRetentionMonths: 12,
      whatsAppPlaceholderNote: "",
    },
  });

  useEffect(() => {
    if (settings) {
      reset({
        organizationName: settings.organizationName,
        organizationContactEmail: settings.organizationContactEmail,
        notifyOnNewVerifiedReport: settings.notifyOnNewVerifiedReport,
        notifyOnCriticalPriority: settings.notifyOnCriticalPriority,
        defaultVerificationSlaHours: settings.defaultVerificationSlaHours,
        duplicateDetectionWindowHours: settings.duplicateDetectionWindowHours,
        reporterDataRetentionMonths: settings.reporterDataRetentionMonths,
        auditLogRetentionMonths: settings.auditLogRetentionMonths,
        whatsAppPlaceholderNote: settings.whatsAppPlaceholderNote ?? "",
      });
    }
  }, [settings, reset]);

  const updateMutation = useMutation({
    mutationFn: (values: SettingsFormValues) => updateSettings(values),
    onSuccess: () => {
      toast.success("Settings saved.");
      queryClient.invalidateQueries({ queryKey: ["settings"] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't save settings."),
  });

  if (admin && !canManage) {
    return (
      <div className="flex flex-col gap-6">
        <PageHeader title="Settings" description="Organisation, notification, and privacy settings." />
        <Alert>
          <ShieldOff className="size-4" />
          <AlertTitle>Access restricted</AlertTitle>
          <AlertDescription>Only Super Admins can view or change settings.</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Settings"
        description={settings ? `Last updated ${formatDateTime(settings.updatedAt)}` : "Organisation, notification, and privacy settings."}
      />

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load settings</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && <Skeleton className="h-96 rounded-lg" />}

      {settings && (
        <form onSubmit={handleSubmit((values) => updateMutation.mutate(values))} noValidate className="flex flex-col gap-4">
          <ChartCard title="Organisation">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="org-name">Organisation name</Label>
                <Input id="org-name" aria-invalid={!!errors.organizationName} {...register("organizationName")} />
                {errors.organizationName && <p className="text-xs text-destructive">{errors.organizationName.message}</p>}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="org-email">Contact email</Label>
                <Input id="org-email" type="email" aria-invalid={!!errors.organizationContactEmail} {...register("organizationContactEmail")} />
                {errors.organizationContactEmail && (
                  <p className="text-xs text-destructive">{errors.organizationContactEmail.message}</p>
                )}
              </div>
            </div>
          </ChartCard>

          <ChartCard title="Notifications">
            <div className="flex flex-col gap-4">
              <Controller
                control={control}
                name="notifyOnNewVerifiedReport"
                render={({ field }) => (
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="text-sm font-medium text-foreground">Notify on new verified report</p>
                      <p className="text-xs text-muted-foreground">Alert administrators when a report passes verification.</p>
                    </div>
                    <Switch checked={field.value} onCheckedChange={field.onChange} />
                  </div>
                )}
              />
              <Controller
                control={control}
                name="notifyOnCriticalPriority"
                render={({ field }) => (
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="text-sm font-medium text-foreground">Notify on Critical priority</p>
                      <p className="text-xs text-muted-foreground">Alert administrators immediately for Critical-priority reports.</p>
                    </div>
                    <Switch checked={field.value} onCheckedChange={field.onChange} />
                  </div>
                )}
              />
            </div>
          </ChartCard>

          <ChartCard title="Verification rules">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="verification-sla">Default verification SLA (hours)</Label>
                <Input id="verification-sla" type="number" min={1} aria-invalid={!!errors.defaultVerificationSlaHours} {...register("defaultVerificationSlaHours")} />
                {errors.defaultVerificationSlaHours && (
                  <p className="text-xs text-destructive">{errors.defaultVerificationSlaHours.message}</p>
                )}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="duplicate-window">Duplicate detection window (hours)</Label>
                <Input id="duplicate-window" type="number" min={1} aria-invalid={!!errors.duplicateDetectionWindowHours} {...register("duplicateDetectionWindowHours")} />
                {errors.duplicateDetectionWindowHours && (
                  <p className="text-xs text-destructive">{errors.duplicateDetectionWindowHours.message}</p>
                )}
              </div>
            </div>
          </ChartCard>

          <ChartCard title="Data retention" description="How long records are kept before eligible for deletion">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="reporter-retention">Reporter data (months)</Label>
                <Input id="reporter-retention" type="number" min={1} aria-invalid={!!errors.reporterDataRetentionMonths} {...register("reporterDataRetentionMonths")} />
                {errors.reporterDataRetentionMonths && (
                  <p className="text-xs text-destructive">{errors.reporterDataRetentionMonths.message}</p>
                )}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="audit-retention">Audit log data (months)</Label>
                <Input id="audit-retention" type="number" min={1} aria-invalid={!!errors.auditLogRetentionMonths} {...register("auditLogRetentionMonths")} />
                {errors.auditLogRetentionMonths && (
                  <p className="text-xs text-destructive">{errors.auditLogRetentionMonths.message}</p>
                )}
              </div>
            </div>
          </ChartCard>

          <ChartCard title="WhatsApp integration" description="Placeholder only — the chatbot itself is not yet built">
            <div className="flex flex-col gap-3">
              <div>
                <Badge variant={settings.whatsAppIntegrationEnabled ? "default" : "secondary"}>
                  <MessageCircle />
                  {settings.whatsAppIntegrationEnabled ? "Enabled" : "Not yet enabled"}
                </Badge>
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="whatsapp-note">Internal note</Label>
                <Textarea
                  id="whatsapp-note"
                  rows={3}
                  placeholder="Notes about the planned WhatsApp integration — see docs/whatsapp-integration-plan.md"
                  {...register("whatsAppPlaceholderNote")}
                />
              </div>
            </div>
          </ChartCard>

          <div className="flex justify-end">
            <Button type="submit" disabled={!isDirty || updateMutation.isPending}>
              {updateMutation.isPending && <Loader2 className="size-4 animate-spin" />}
              Save settings
            </Button>
          </div>
        </form>
      )}
    </div>
  );
}
