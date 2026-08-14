"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { reasonRequiredFor, verificationDecisionSchema } from "@/lib/validation/reportSchemas";
import { VerificationDecisionAction } from "@/types/enums";

const actionCopy: Record<VerificationDecisionAction, { title: string; description: string; destructive: boolean }> = {
  [VerificationDecisionAction.Approve]: {
    title: "Approve report",
    description: "This report will move into the operational queue as Verified, Under Review.",
    destructive: false,
  },
  [VerificationDecisionAction.Reject]: {
    title: "Reject report",
    description: "This report will be marked Rejected and will not enter the operational queue.",
    destructive: true,
  },
  [VerificationDecisionAction.RequestClarification]: {
    title: "Request clarification",
    description: "This report will be marked as needing clarification from the reporter.",
    destructive: false,
  },
  [VerificationDecisionAction.MarkDuplicate]: {
    title: "Mark as duplicate",
    description: "This report will be marked a suspected duplicate of an existing case.",
    destructive: true,
  },
  [VerificationDecisionAction.Escalate]: {
    title: "Escalate report",
    description: "This report will be flagged for further review as potential abuse or a false report.",
    destructive: true,
  },
};

interface VerificationDecisionDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  caseReference: string;
  action: VerificationDecisionAction;
  isSubmitting: boolean;
  onConfirm: (reason: string | undefined) => void;
}

export function VerificationDecisionDialog({
  open,
  onOpenChange,
  caseReference,
  action,
  isSubmitting,
  onConfirm,
}: VerificationDecisionDialogProps) {
  const copy = actionCopy[action];
  const reasonRequired = reasonRequiredFor(action);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(verificationDecisionSchema),
    defaultValues: { action, reason: "" },
  });

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) reset({ action, reason: "" });
        onOpenChange(next);
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{copy.title}</DialogTitle>
          <DialogDescription>
            {copy.description} Case {caseReference}.
          </DialogDescription>
        </DialogHeader>

        <form
          onSubmit={handleSubmit((values) => onConfirm(values.reason || undefined))}
          noValidate
          className="flex flex-col gap-3"
        >
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="decision-reason">
              Reason {reasonRequired ? "" : "(optional)"}
            </Label>
            <Textarea
              id="decision-reason"
              placeholder="Recorded on the report's audit trail…"
              aria-invalid={!!errors.reason}
              {...register("reason")}
            />
            {errors.reason && <p className="text-xs text-destructive">{errors.reason.message}</p>}
          </div>

          <DialogFooter>
            <Button type="submit" variant={copy.destructive ? "destructive" : "default"} disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="size-4 animate-spin" />}
              Confirm
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
