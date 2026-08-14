"use client";

import { useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
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
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { updateReportSchema, type UpdateReportFormValues } from "@/lib/validation/reportSchemas";
import { IncidentPriority } from "@/types/enums";
import type { Category } from "@/types/categories";
import type { IncidentReportDetail } from "@/types/reports";

interface EditReportDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  report: IncidentReportDetail;
  categories: Category[];
  isSubmitting: boolean;
  onSave: (values: UpdateReportFormValues) => void;
}

export function EditReportDialog({
  open,
  onOpenChange,
  report,
  categories,
  isSubmitting,
  onSave,
}: EditReportDialogProps) {
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<UpdateReportFormValues>({
    resolver: zodResolver(updateReportSchema),
    defaultValues: {
      categoryId: report.categoryId,
      priority: report.priority,
      locationDescription: report.locationDescription,
      description: report.description,
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        categoryId: report.categoryId,
        priority: report.priority,
        locationDescription: report.locationDescription,
        description: report.description,
      });
    }
  }, [open, report, reset]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Edit report details</DialogTitle>
          <DialogDescription>Update the category, priority, location, and description.</DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSave)} noValidate className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label>Category</Label>
            <Controller
              control={control}
              name="categoryId"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {categories.map((c) => (
                      <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.categoryId && <p className="text-xs text-destructive">{errors.categoryId.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label>Priority</Label>
            <Controller
              control={control}
              name="priority"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger className="w-full"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {Object.values(IncidentPriority).map((p) => (
                      <SelectItem key={p} value={p}>{p}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-location">Location</Label>
            <Input id="edit-location" aria-invalid={!!errors.locationDescription} {...register("locationDescription")} />
            {errors.locationDescription && (
              <p className="text-xs text-destructive">{errors.locationDescription.message}</p>
            )}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="edit-description">Description</Label>
            <Textarea
              id="edit-description"
              rows={4}
              aria-invalid={!!errors.description}
              {...register("description")}
            />
            {errors.description && <p className="text-xs text-destructive">{errors.description.message}</p>}
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
