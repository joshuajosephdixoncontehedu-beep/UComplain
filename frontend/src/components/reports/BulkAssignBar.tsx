"use client";

import { XIcon } from "lucide-react";
import { Button } from "@/components/ui/button";

interface BulkAssignBarProps {
  selectedCount: number;
  onClear: () => void;
  onAssign: () => void;
}

export function BulkAssignBar({ selectedCount, onClear, onAssign }: BulkAssignBarProps) {
  if (selectedCount === 0) return null;

  return (
    <div className="flex items-center justify-between rounded-lg border border-primary/20 bg-primary/5 px-4 py-2.5">
      <p className="text-sm text-foreground">
        <span className="font-medium">{selectedCount}</span> report{selectedCount === 1 ? "" : "s"} selected
      </p>
      <div className="flex items-center gap-2">
        <Button size="sm" onClick={onAssign}>Assign to…</Button>
        <Button size="sm" variant="ghost" onClick={onClear}>
          <XIcon />
          Clear selection
        </Button>
      </div>
    </div>
  );
}
