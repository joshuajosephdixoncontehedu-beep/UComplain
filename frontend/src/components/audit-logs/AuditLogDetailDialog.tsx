"use client";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { humanizePascalCase } from "@/lib/utils/statusStyles";
import { formatDateTime } from "@/lib/utils/format";
import type { AuditLogListItem } from "@/types/auditLogs";

function formatJson(value: string | null): string | null {
  if (!value) return null;
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

export function AuditLogDetailDialog({
  entry,
  onOpenChange,
}: {
  entry: AuditLogListItem | null;
  onOpenChange: (open: boolean) => void;
}) {
  const previous = entry ? formatJson(entry.previousValueJson) : null;
  const next = entry ? formatJson(entry.newValueJson) : null;

  return (
    <Dialog open={!!entry} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{entry ? humanizePascalCase(entry.action) : ""}</DialogTitle>
          <DialogDescription>
            {entry?.adminUserName ?? "System"} · {entry ? formatDateTime(entry.createdAt) : ""}
          </DialogDescription>
        </DialogHeader>

        {entry && (
          <div className="flex flex-col gap-3 text-sm">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <p className="text-xs text-muted-foreground">Entity</p>
                <p>{entry.entityType} <span className="text-muted-foreground">({entry.entityId})</span></p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">IP address</p>
                <p>{entry.ipAddress ?? "—"}</p>
              </div>
            </div>

            {entry.userAgent && (
              <div>
                <p className="text-xs text-muted-foreground">User agent</p>
                <p className="text-xs break-all text-muted-foreground">{entry.userAgent}</p>
              </div>
            )}

            {(previous || next) && (
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                {previous && (
                  <div>
                    <p className="text-xs text-muted-foreground">Before</p>
                    <pre className="mt-1 max-h-48 overflow-auto rounded-md bg-muted p-2 text-xs">{previous}</pre>
                  </div>
                )}
                {next && (
                  <div>
                    <p className="text-xs text-muted-foreground">After</p>
                    <pre className="mt-1 max-h-48 overflow-auto rounded-md bg-muted p-2 text-xs">{next}</pre>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
