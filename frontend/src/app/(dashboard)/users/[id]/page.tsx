"use client";

import { use, useState } from "react";
import Link from "next/link";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ArrowLeft, ShieldAlert, ShieldCheck } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/ui/status-badge";
import { ChartCard } from "@/components/dashboard/ChartCard";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ReporterVerificationHistoryList } from "@/components/users/ReporterVerificationHistoryList";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isManagerOrAbove } from "@/lib/auth/permissions";
import { getReporter, restrictReporter, unrestrictReporter } from "@/lib/api/reporters";
import { ApiError } from "@/lib/api/client";
import { caseStatusTone, verificationStatusTone } from "@/lib/utils/statusStyles";
import { formatDateTime } from "@/lib/utils/format";

export default function ReporterDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { admin } = useAuth();
  const queryClient = useQueryClient();
  const canRestrict = admin ? isManagerOrAbove(admin.role) : false;
  const [confirmingRestriction, setConfirmingRestriction] = useState(false);

  const { data: reporter, isLoading, isError, error } = useQuery({
    queryKey: ["reporter", id],
    queryFn: () => getReporter(id),
  });

  const restrictMutation = useMutation({
    mutationFn: () => (reporter?.isRestricted ? unrestrictReporter(id) : restrictReporter(id)),
    onSuccess: () => {
      toast.success(reporter?.isRestricted ? "Reporter unrestricted." : "Reporter restricted.");
      setConfirmingRestriction(false);
      queryClient.invalidateQueries({ queryKey: ["reporter", id] });
      queryClient.invalidateQueries({ queryKey: ["reporters"] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't update this reporter."),
  });

  return (
    <div className="flex flex-col gap-6">
      <Link href="/users" className="inline-flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-3.5" />
        Back to users
      </Link>

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load this user</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && <Skeleton className="h-64 rounded-lg" />}

      {reporter && (
        <>
          <PageHeader
            title={reporter.maskedContactReference}
            description={`First seen ${formatDateTime(reporter.createdAt)}`}
            actions={
              canRestrict && (
                <Button
                  size="sm"
                  variant={reporter.isRestricted ? "outline" : "destructive"}
                  onClick={() => setConfirmingRestriction(true)}
                >
                  {reporter.isRestricted ? <ShieldCheck /> : <ShieldAlert />}
                  {reporter.isRestricted ? "Remove restriction" : "Restrict reporter"}
                </Button>
              )
            }
          />

          <div className="flex flex-wrap items-center gap-2">
            <StatusBadge value={reporter.verificationStatus} tone={verificationStatusTone(reporter.verificationStatus)} />
            {reporter.isRestricted && <Badge variant="destructive"><ShieldAlert />Restricted</Badge>}
            <Badge variant="outline">
              {reporter.consentAt ? `Consent given ${formatDateTime(reporter.consentAt)}` : "No consent on record"}
            </Badge>
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <ChartCard title="Reports" className="lg:col-span-2">
              {reporter.reports.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted-foreground">No reports from this user yet.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Case</TableHead>
                      <TableHead>Category</TableHead>
                      <TableHead>Verification</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Reported</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {reporter.reports.map((r) => (
                      <TableRow key={r.id}>
                        <TableCell>
                          <Link href={`/reports/${r.id}`} className="font-medium text-primary hover:underline">
                            {r.caseReference}
                          </Link>
                        </TableCell>
                        <TableCell>{r.categoryName}</TableCell>
                        <TableCell><StatusBadge value={r.verificationStatus} tone={verificationStatusTone(r.verificationStatus)} /></TableCell>
                        <TableCell><StatusBadge value={r.caseStatus} tone={caseStatusTone(r.caseStatus)} /></TableCell>
                        <TableCell className="text-muted-foreground">{formatDateTime(r.createdAt)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </ChartCard>

            <ChartCard title="Verification history">
              <ReporterVerificationHistoryList items={reporter.verificationHistory} />
            </ChartCard>
          </div>

          <AlertDialog open={confirmingRestriction} onOpenChange={setConfirmingRestriction}>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>
                  {reporter.isRestricted ? "Remove restriction?" : "Restrict this reporter?"}
                </AlertDialogTitle>
                <AlertDialogDescription>
                  {reporter.isRestricted
                    ? "Future reports from this reporter will be treated normally again."
                    : "Future reports from this reporter will be flagged for closer review. Existing reports are unaffected."}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  variant={reporter.isRestricted ? "default" : "destructive"}
                  disabled={restrictMutation.isPending}
                  onClick={() => restrictMutation.mutate()}
                >
                  {reporter.isRestricted ? "Remove restriction" : "Restrict"}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </>
      )}
    </div>
  );
}
