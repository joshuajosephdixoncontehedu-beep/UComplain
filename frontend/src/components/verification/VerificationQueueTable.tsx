"use client";

import Link from "next/link";
import { ChevronDown } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { StatusBadge } from "@/components/ui/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { priorityTone } from "@/lib/utils/statusStyles";
import { formatRelativeTime } from "@/lib/utils/format";
import { verificationAge } from "@/lib/utils/verificationAge";
import { badgeToneClasses } from "@/lib/utils/statusStyles";
import { VerificationDecisionAction } from "@/types/enums";
import type { VerificationQueueItem } from "@/types/verification";

const destructiveActions = new Set<VerificationDecisionAction>([
  VerificationDecisionAction.Reject,
  VerificationDecisionAction.MarkDuplicate,
  VerificationDecisionAction.Escalate,
]);

const actionLabels: Record<VerificationDecisionAction, string> = {
  [VerificationDecisionAction.Approve]: "Approve",
  [VerificationDecisionAction.Reject]: "Reject",
  [VerificationDecisionAction.RequestClarification]: "Request clarification",
  [VerificationDecisionAction.MarkDuplicate]: "Mark as duplicate",
  [VerificationDecisionAction.Escalate]: "Escalate",
};

interface VerificationQueueTableProps {
  items: VerificationQueueItem[];
  canDecide: boolean;
  onDecide: (item: VerificationQueueItem, action: VerificationDecisionAction) => void;
}

export function VerificationQueueTable({ items, canDecide, onDecide }: VerificationQueueTableProps) {
  if (items.length === 0) {
    return (
      <div className="rounded-lg border border-border bg-card py-12 text-center text-sm text-muted-foreground">
        Nothing in this tab.
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-border bg-card">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Case</TableHead>
            <TableHead>Category</TableHead>
            <TableHead>Location</TableHead>
            <TableHead>Priority</TableHead>
            <TableHead>Reporter</TableHead>
            <TableHead>Attempts</TableHead>
            <TableHead>Age</TableHead>
            {canDecide && <TableHead className="text-right">Decision</TableHead>}
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => {
            const age = verificationAge(item.createdAt, item.categorySlaHours);
            return (
              <TableRow key={item.id}>
                <TableCell>
                  <Link href={`/reports/${item.id}`} className="font-medium text-primary hover:underline">
                    {item.caseReference}
                  </Link>
                </TableCell>
                <TableCell>{item.categoryName}</TableCell>
                <TableCell className="max-w-40 truncate">{item.locationDescription}</TableCell>
                <TableCell><StatusBadge value={item.priority} tone={priorityTone(item.priority)} /></TableCell>
                <TableCell className="text-muted-foreground">{item.reporterMaskedContact}</TableCell>
                <TableCell className="text-muted-foreground">{item.attemptCount}</TableCell>
                <TableCell>
                  <div className="flex flex-col gap-0.5">
                    <Badge variant="outline" className={badgeToneClasses[age.tone]}>{age.label}</Badge>
                    <span className="text-xs text-muted-foreground">{formatRelativeTime(item.createdAt)}</span>
                  </div>
                </TableCell>
                {canDecide && (
                  <TableCell className="text-right">
                    <DropdownMenu>
                      <DropdownMenuTrigger render={<Button size="sm" variant="outline" />}>
                        Decide
                        <ChevronDown />
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        {Object.values(VerificationDecisionAction).map((action) => (
                          <DropdownMenuItem
                            key={action}
                            variant={destructiveActions.has(action) ? "destructive" : "default"}
                            onClick={() => onDecide(item, action)}
                          >
                            {actionLabels[action]}
                          </DropdownMenuItem>
                        ))}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                )}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}
