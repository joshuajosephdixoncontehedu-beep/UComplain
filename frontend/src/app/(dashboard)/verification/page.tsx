"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { VerificationQueueTable } from "@/components/verification/VerificationQueueTable";
import { VerificationDecisionDialog } from "@/components/verification/VerificationDecisionDialog";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isReviewerOrAbove } from "@/lib/auth/permissions";
import { getVerificationQueue, submitVerificationDecision } from "@/lib/api/verification";
import { ApiError } from "@/lib/api/client";
import { VerificationDecisionAction } from "@/types/enums";
import type { VerificationQueueItem, VerificationQueueResponse } from "@/types/verification";

const tabs: { key: keyof VerificationQueueResponse; label: string }[] = [
  { key: "pending", label: "Pending" },
  { key: "needsClarification", label: "Needs Clarification" },
  { key: "suspectedDuplicate", label: "Suspected Duplicate" },
  { key: "flaggedAbuse", label: "Flagged Abuse" },
  { key: "rejected", label: "Rejected" },
];

export default function VerificationPage() {
  const { admin } = useAuth();
  const queryClient = useQueryClient();
  const canDecide = admin ? isReviewerOrAbove(admin.role) : false;

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["verification-queue"],
    queryFn: getVerificationQueue,
  });
  const [pendingDecision, setPendingDecision] = useState<{
    item: VerificationQueueItem;
    action: VerificationDecisionAction;
  } | null>(null);

  const decisionMutation = useMutation({
    mutationFn: ({ reportId, action, reason }: { reportId: string; action: VerificationDecisionAction; reason?: string }) =>
      submitVerificationDecision(reportId, { action, reason }),
    onSuccess: () => {
      toast.success("Verification decision recorded.");
      setPendingDecision(null);
      queryClient.invalidateQueries({ queryKey: ["verification-queue"] });
      queryClient.invalidateQueries({ queryKey: ["reports"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't record this decision."),
  });

  if (admin && !canDecide) {
    return (
      <div className="flex flex-col gap-6">
        <PageHeader title="Verification Queue" description="Reports awaiting a human verification decision." />
        <Alert>
          <ShieldOff className="size-4" />
          <AlertTitle>Access restricted</AlertTitle>
          <AlertDescription>Your role doesn&apos;t have permission to view the verification queue.</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Verification Queue"
        description="Reports awaiting a human verification decision — not yet active operational cases."
      />

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load the verification queue</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && <Skeleton className="h-96 rounded-lg" />}

      {data && (
        <Tabs defaultValue="pending">
          <TabsList>
            {tabs.map((tab) => (
              <TabsTrigger key={tab.key} value={tab.key} className="gap-1.5">
                {tab.label}
                <Badge variant="secondary" className="h-4 px-1.5 text-[10px]">{data[tab.key].length}</Badge>
              </TabsTrigger>
            ))}
          </TabsList>

          {tabs.map((tab) => (
            <TabsContent key={tab.key} value={tab.key} className="mt-4">
              <VerificationQueueTable
                items={data[tab.key]}
                canDecide={canDecide}
                onDecide={(item, action) => setPendingDecision({ item, action })}
              />
            </TabsContent>
          ))}
        </Tabs>
      )}

      {pendingDecision && (
        <VerificationDecisionDialog
          open={!!pendingDecision}
          onOpenChange={(open) => !open && setPendingDecision(null)}
          caseReference={pendingDecision.item.caseReference}
          action={pendingDecision.action}
          isSubmitting={decisionMutation.isPending}
          onConfirm={(reason) =>
            decisionMutation.mutate({ reportId: pendingDecision.item.id, action: pendingDecision.action, reason })
          }
        />
      )}
    </div>
  );
}
