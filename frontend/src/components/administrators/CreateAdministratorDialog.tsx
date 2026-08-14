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
import { createAdministratorSchema, type CreateAdministratorFormValues } from "@/lib/validation/administratorSchemas";
import { roleDescription, roleLabel } from "@/lib/auth/permissions";
import { AdminRole } from "@/types/enums";

interface CreateAdministratorDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  isSubmitting: boolean;
  onSave: (values: CreateAdministratorFormValues) => void;
}

const defaults: CreateAdministratorFormValues = {
  fullName: "",
  email: "",
  role: AdminRole.Reviewer,
  temporaryPassword: "",
};

export function CreateAdministratorDialog({ open, onOpenChange, isSubmitting, onSave }: CreateAdministratorDialogProps) {
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<CreateAdministratorFormValues>({
    resolver: zodResolver(createAdministratorSchema),
    defaultValues: defaults,
  });

  useEffect(() => {
    if (open) reset(defaults);
  }, [open, reset]);

  const selectedRole = useWatch({ control, name: "role" });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Add administrator</DialogTitle>
          <DialogDescription>
            They&apos;ll sign in with this temporary password and should change it on first login.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSave)} noValidate className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="admin-name">Full name</Label>
            <Input id="admin-name" aria-invalid={!!errors.fullName} {...register("fullName")} />
            {errors.fullName && <p className="text-xs text-destructive">{errors.fullName.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="admin-email">Email</Label>
            <Input id="admin-email" type="email" aria-invalid={!!errors.email} {...register("email")} />
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

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="admin-password">Temporary password</Label>
            <Input
              id="admin-password"
              type="text"
              autoComplete="off"
              aria-invalid={!!errors.temporaryPassword}
              {...register("temporaryPassword")}
            />
            {errors.temporaryPassword && (
              <p className="text-xs text-destructive">{errors.temporaryPassword.message}</p>
            )}
          </div>

          <DialogFooter>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="size-4 animate-spin" />}
              Add administrator
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
