"use client";

import { useState } from "react";
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { Administrator } from "@/types/administrators";

interface AssignAdminDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  administrators: Administrator[];
  isSubmitting: boolean;
  onConfirm: (adminUserId: string) => void;
}

export function AssignAdminDialog({
  open,
  onOpenChange,
  title,
  description,
  administrators,
  isSubmitting,
  onConfirm,
}: AssignAdminDialogProps) {
  const [adminUserId, setAdminUserId] = useState<string | undefined>(undefined);

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) setAdminUserId(undefined);
        onOpenChange(next);
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-1.5">
          <Label>Administrator</Label>
          <Select value={adminUserId} onValueChange={(value) => setAdminUserId(value ?? undefined)}>
            <SelectTrigger className="w-full"><SelectValue placeholder="Choose an administrator" /></SelectTrigger>
            <SelectContent>
              {administrators.map((a) => (
                <SelectItem key={a.id} value={a.id}>{a.fullName}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <DialogFooter>
          <Button
            disabled={!adminUserId || isSubmitting}
            onClick={() => adminUserId && onConfirm(adminUserId)}
          >
            {isSubmitting && <Loader2 className="size-4 animate-spin" />}
            Assign
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
