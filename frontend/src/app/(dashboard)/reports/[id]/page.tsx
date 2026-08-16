"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ArrowLeft, Info, Pencil } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/ui/status-badge";
import { ChartCard } from "@/components/dashboard/ChartCard";
import { RecentActivityTimeline } from "@/components/dashboard/RecentActivityTimeline";
import { AttachmentsList } from "@/components/reports/detail/AttachmentsList";
import { ReportOverviewCard } from "@/components/reports/detail/ReportOverviewCard";
import { ReporterCard } from "@/components/reports/detail/ReporterCard";
import { AssignmentCard } from "@/components/reports/detail/AssignmentCard";
import { StatusControlCard } from "@/components/reports/detail/StatusControlCard";
import { VerificationHistoryList } from "@/components/reports/detail/VerificationHistoryList";
import { StatusHistoryList } from "@/components/reports/detail/StatusHistoryList";
import { NotesSection } from "@/components/reports/detail/NotesSection";
import { EditReportDialog } from "@/components/reports/detail/EditReportDialog";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isManagerOrAbove, isReviewerOrAbove } from "@/lib/auth/permissions";
import { getReport, updateReport, assignReport, addReportNote, changeReportStatus } from "@/lib/api/reports";
import { getCategories } from "@/lib/api/categories";
import { getAdministrators } from "@/lib/api/administrators";
import { ApiError } from "@/lib/api/client";
import { caseStatusTone, priorityTone, verificationStatusTone } from "@/lib/utils/statusStyles";
import type { UpdateReportFormValues } from "@/lib/validation/reportSchemas";
import { VerificationStatus, type CaseStatus } from "@/types/enums";

export default function ReportDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { admin } = useAuth();
  const queryClient = useQueryClient();
  const [editOpen, setEditOpen] = useState(false);

  const canEdit = admin ? isReviewerOrAbove(admin.role) : false;
  const canAssign = admin ? isManagerOrAbove(admin.role) : false;
  const canAddNotes = admin ? isReviewerOrAbove(admin.role) : false;
  const canChangeStatus = admin ? isReviewerOrAbove(admin.role) : false;

  const { data: report, isLoading, isError, error } = useQuery({
    queryKey: ["report", id],
    queryFn: () => getReport(id),
  });
  const { data: categories } = useQuery({ queryKey: ["categories"], queryFn: getCategories });
  const { data: administrators } = useQuery({ queryKey: ["administrators"], queryFn: getAdministrators });

  const invalidateReport = () => {
    queryClient.invalidateQueries({ queryKey: ["report", id] });
    queryClient.invalidateQueries({ queryKey: ["reports"] });
  };

  const updateMutation = useMutation({
    mutationFn: (values: UpdateReportFormValues) => updateReport(id, values),
    onSuccess: () => {
      toast.success("Report details updated.");
      setEditOpen(false);
      invalidateReport();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't update the report."),
  });

  const assignMutation = useMutation({
    mutationFn: (adminUserId: string) => assignReport(id, { adminUserId }),
    onSuccess: () => {
      toast.success("Report assigned.");
      invalidateReport();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't assign the report."),
  });

  const noteMutation = useMutation({
    mutationFn: (content: string) => addReportNote(id, { content }),
    onSuccess: () => {
      toast.success("Note added.");
      invalidateReport();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't add the note."),
  });

  const statusMutation = useMutation({
    mutationFn: ({ newStatus, notes }: { newStatus: CaseStatus; notes: string }) =>
      changeReportStatus(id, { newStatus, notes: notes || undefined }),
    onSuccess: () => {
      toast.success("Status updated.");
      invalidateReport();
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't change the status."),
  });

  return (
    <div className="flex flex-col gap-6">
      <Link href="/reports" className="inline-flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-3.5" />
        Back to reports
      </Link>

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load this report</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && (
        <div className="flex flex-col gap-4">
          <Skeleton className="h-16 rounded-lg" />
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <Skeleton className="h-64 rounded-lg lg:col-span-2" />
            <Skeleton className="h-64 rounded-lg" />
          </div>
        </div>
      )}

      {report && (
        <>
          <PageHeader
            title={report.caseReference}
            description={report.locationDescription}
            actions={
              canEdit && (
                <Button size="sm" variant="outline" onClick={() => setEditOpen(true)}>
                  <Pencil />
                  Edit details
                </Button>
              )
            }
          />

          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge value={report.priority} tone={priorityTone(report.priority)} />
            <StatusBadge value={report.caseStatus} tone={caseStatusTone(report.caseStatus)} />
            <StatusBadge value={report.verificationStatus} tone={verificationStatusTone(report.verificationStatus)} />
          </div>

          {report.verificationStatus !== VerificationStatus.Verified && (
            <Alert>
              <Info className="size-4" />
              <AlertTitle>Not yet verified</AlertTitle>
              <AlertDescription>
                This report has not completed verification, so assignment and status controls are unavailable here.
                Decisions are made from the{" "}
                <Link href="/verification" className="underline underline-offset-2">
                  verification queue
                </Link>
                .
              </AlertDescription>
            </Alert>
          )}

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <div className="flex flex-col gap-4 lg:col-span-2">
              <ChartCard title="Report details">
                <ReportOverviewCard report={report} />
              </ChartCard>

              <ChartCard title="Attachments" description="Photos, audio, and other files the reporter attached">
                <AttachmentsList reportId={report.id} items={report.mediaAttachments} />
              </ChartCard>

              <ChartCard title="Verification history">
                <VerificationHistoryList items={report.verificationHistory} />
              </ChartCard>

              <ChartCard title="Status history">
                <StatusHistoryList items={report.statusHistory} />
              </ChartCard>

              <ChartCard title="Internal notes" description="Visible to admins only">
                <NotesSection
                  notes={report.notes}
                  canAddNotes={canAddNotes}
                  isSubmitting={noteMutation.isPending}
                  onAddNote={(content) => noteMutation.mutate(content)}
                />
              </ChartCard>

              <ChartCard title="Audit trail">
                <RecentActivityTimeline data={report.auditTrail} />
              </ChartCard>
            </div>

            <div className="flex flex-col gap-4">
              <ChartCard title="Reporter">
                <ReporterCard maskedContact={report.reporterMaskedContact} isRestricted={report.reporterIsRestricted} />
              </ChartCard>

              {report.verificationStatus === VerificationStatus.Verified && (
                <>
                  <ChartCard title="Status">
                    <StatusControlCard
                      currentStatus={report.caseStatus}
                      canChangeStatus={canChangeStatus}
                      isSubmitting={statusMutation.isPending}
                      onChangeStatus={(newStatus, notes) => statusMutation.mutate({ newStatus, notes })}
                    />
                  </ChartCard>

                  <ChartCard title="Assignment">
                    <AssignmentCard
                      assignedAdminName={report.assignedAdminName}
                      assignments={report.assignments}
                      administrators={administrators ?? []}
                      canAssign={canAssign}
                      isSubmitting={assignMutation.isPending}
                      onAssign={(adminUserId) => assignMutation.mutate(adminUserId)}
                    />
                  </ChartCard>
                </>
              )}
            </div>
          </div>

          <EditReportDialog
            open={editOpen}
            onOpenChange={setEditOpen}
            report={report}
            categories={categories ?? []}
            isSubmitting={updateMutation.isPending}
            onSave={(values) => updateMutation.mutate(values)}
          />
        </>
      )}
    </div>
  );
}
