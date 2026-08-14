"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { StatusBadge } from "@/components/ui/status-badge";
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
import { getAllowedNextStatuses } from "@/lib/utils/caseStatusTransitions";
import { caseStatusTone, humanizeEnumValue } from "@/lib/utils/statusStyles";
import { CaseStatus } from "@/types/enums";

interface StatusControlCardProps {
  currentStatus: CaseStatus;
  canChangeStatus: boolean;
  isSubmitting: boolean;
  onChangeStatus: (newStatus: CaseStatus, notes: string) => void;
}

// High-impact terminal transitions get an explicit warning beyond the standard
// confirmation — closing or resolving a case is hard for a reporter-facing outcome to
// walk back informally, unlike moving between working states.
const highImpactStatuses = new Set<CaseStatus>([CaseStatus.Resolved, CaseStatus.Closed]);

export function StatusControlCard({
  currentStatus,
  canChangeStatus,
  isSubmitting,
  onChangeStatus,
}: StatusControlCardProps) {
  const [pendingStatus, setPendingStatus] = useState<CaseStatus | null>(null);
  const [notes, setNotes] = useState("");

  const nextStatuses = getAllowedNextStatuses(currentStatus);

  return (
    <div className="flex flex-col gap-4">
      <div>
        <p className="text-xs text-muted-foreground">Current status</p>
        <StatusBadge value={currentStatus} tone={caseStatusTone(currentStatus)} className="mt-1" />
      </div>

      {canChangeStatus && nextStatuses.length > 0 && (
        <div className="flex flex-col gap-2">
          <p className="text-xs text-muted-foreground">Move to</p>
          <div className="flex flex-wrap gap-2">
            {nextStatuses.map((status) => (
              <Button
                key={status}
                size="sm"
                variant="outline"
                onClick={() => {
                  setPendingStatus(status);
                  setNotes("");
                }}
              >
                {humanizeEnumValue(status)}
              </Button>
            ))}
          </div>
        </div>
      )}

      <AlertDialog open={pendingStatus !== null} onOpenChange={(open) => !open && setPendingStatus(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              Move to {pendingStatus ? humanizeEnumValue(pendingStatus) : ""}?
            </AlertDialogTitle>
            <AlertDialogDescription>
              {pendingStatus && highImpactStatuses.has(pendingStatus)
                ? "This is a high-impact change. It will be recorded in the case's status history and audit trail."
                : "This change will be recorded in the case's status history and audit trail."}
            </AlertDialogDescription>
          </AlertDialogHeader>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="status-notes">Notes (optional)</Label>
            <Textarea
              id="status-notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Add context for this status change…"
            />
          </div>

          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              disabled={isSubmitting}
              onClick={() => {
                if (pendingStatus) onChangeStatus(pendingStatus, notes);
                setPendingStatus(null);
              }}
            >
              {isSubmitting && <Loader2 className="size-4 animate-spin" />}
              Confirm
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
