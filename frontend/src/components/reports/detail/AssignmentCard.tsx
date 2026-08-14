"use client";

import { useState } from "react";
import { UserPlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { AssignAdminDialog } from "@/components/reports/AssignAdminDialog";
import { formatDateTime } from "@/lib/utils/format";
import type { Administrator } from "@/types/administrators";
import type { ReportAssignmentItem } from "@/types/reports";

interface AssignmentCardProps {
  assignedAdminName: string | null;
  assignments: ReportAssignmentItem[];
  administrators: Administrator[];
  canAssign: boolean;
  isSubmitting: boolean;
  onAssign: (adminUserId: string) => void;
}

export function AssignmentCard({
  assignedAdminName,
  assignments,
  administrators,
  canAssign,
  isSubmitting,
  onAssign,
}: AssignmentCardProps) {
  const [open, setOpen] = useState(false);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-2">
        <div>
          <p className="text-xs text-muted-foreground">Currently assigned to</p>
          <p className="text-sm font-medium text-foreground">{assignedAdminName ?? "Unassigned"}</p>
        </div>
        {canAssign && (
          <Button size="sm" variant="outline" onClick={() => setOpen(true)}>
            <UserPlus />
            {assignedAdminName ? "Reassign" : "Assign"}
          </Button>
        )}
      </div>

      {assignments.length > 0 && (
        <ul className="flex flex-col gap-2 border-t border-border pt-3">
          {assignments.map((a) => (
            <li key={a.id} className="text-xs text-muted-foreground">
              <span className="font-medium text-foreground">{a.adminUserName}</span> assigned by{" "}
              {a.assignedByAdminName} on {formatDateTime(a.assignedAt)}
              {a.unassignedAt && <> · unassigned {formatDateTime(a.unassignedAt)}</>}
            </li>
          ))}
        </ul>
      )}

      <AssignAdminDialog
        open={open}
        onOpenChange={setOpen}
        title={assignedAdminName ? "Reassign report" : "Assign report"}
        description="Choose the administrator responsible for this report."
        administrators={administrators}
        isSubmitting={isSubmitting}
        onConfirm={(adminUserId) => {
          onAssign(adminUserId);
          setOpen(false);
        }}
      />
    </div>
  );
}
