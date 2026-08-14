"use client";

import { useEffect } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
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
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { updateAdministratorSchema, type UpdateAdministratorFormValues } from "@/lib/validation/administratorSchemas";
import { roleDescription, roleLabel } from "@/lib/auth/permissions";
import { AdminRole } from "@/types/enums";
import type { Administrator } from "@/types/administrators";

interface EditAdministratorDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  administrator: Administrator | null;
  isSubmitting: boolean;
  onSave: (values: UpdateAdministratorFormValues) => void;
}

export function EditAdministratorDialog({
  open,
  onOpenChange,
  administrator,
  isSubmitting,
  onSave,
}: EditAdministratorDialogProps) {
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<UpdateAdministratorFormValues>({
    resolver: zodResolver(updateAdministratorSchema),
    defaultValues: { fullName: "", email: "", role: AdminRole.Reviewer },
  });

  useEffect(() => {
    if (open && administrator) {
      reset({ fullName: administrator.fullName, email: administrator.email, role: administrator.role });
    }
  }, [open, administrator, reset]);

  const selectedRole = useWatch({ control, name: "role" });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Edit administrator</DialogTitle>
          <DialogDescription>Update this administrator&apos;s name, email, or role.</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSave)} noValidate className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-admin-name">Full name</Label>
            <Input id="edit-admin-name" aria-invalid={!!errors.fullName} {...register("fullName")} />
            {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-admin-email">Email</Label>
            <Input id="edit-admin-email" type="email" aria-invalid={!!errors.email} {...register("email")} />
            {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>Role</Label>
            <Controller
              control={control}
              name="role"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {Object.values(AdminRole).map((r) => (
                      <SelectItem key={r} value={r}>{roleLabel(r)}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            <p className="text-xs text-muted-foreground">{roleDescription(selectedRole)}</p>
          </div>

          <DialogFooter>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="size-4 animate-spin" />}
              Save changes
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
