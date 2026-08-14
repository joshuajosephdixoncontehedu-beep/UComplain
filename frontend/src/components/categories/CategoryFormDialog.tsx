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
import { categoryFormSchema, type CategoryFormInput, type CategoryFormValues } from "@/lib/validation/categorySchemas";
import { IncidentPriority } from "@/types/enums";
import type { Category } from "@/types/categories";

interface CategoryFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  category: Category | null;
  isSubmitting: boolean;
  onSave: (values: CategoryFormValues) => void;
}

const emptyDefaults: CategoryFormInput = {
  name: "",
  description: "",
  defaultPriority: IncidentPriority.Medium,
  slaHours: 24,
  displayOrder: 0,
};

export function CategoryFormDialog({ open, onOpenChange, category, isSubmitting, onSave }: CategoryFormDialogProps) {
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<CategoryFormInput, unknown, CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: emptyDefaults,
  });

  useEffect(() => {
    if (open) {
      reset(
        category
          ? {
              name: category.name,
              description: category.description,
              defaultPriority: category.defaultPriority,
              slaHours: category.slaHours,
              displayOrder: category.displayOrder,
            }
          : emptyDefaults,
      );
    }
  }, [open, category, reset]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{category ? "Edit category" : "Add category"}</DialogTitle>
          <DialogDescription>
            Categories set the default priority and SLA clock for new reports in this area.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSave)} noValidate className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="category-name">Name</Label>
            <Input id="category-name" aria-invalid={!!errors.name} {...register("name")} />
            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="category-description">Description</Label>
            <Textarea id="category-description" rows={3} aria-invalid={!!errors.description} {...register("description")} />
            {errors.description && <p className="text-xs text-destructive">{errors.description.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label>Default priority</Label>
              <Controller
                control={control}
                name="defaultPriority"
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
              <Label htmlFor="category-sla">SLA (hours)</Label>
              <Input
                id="category-sla"
                type="number"
                min={1}
                aria-invalid={!!errors.slaHours}
                {...register("slaHours")}
              />
              {errors.slaHours && <p className="text-xs text-destructive">{errors.slaHours.message}</p>}
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="category-order">Display order</Label>
            <Input
              id="category-order"
              type="number"
              min={0}
              aria-invalid={!!errors.displayOrder}
              {...register("displayOrder")}
            />
            {errors.displayOrder && <p className="text-xs text-destructive">{errors.displayOrder.message}</p>}
          </div>

          <DialogFooter>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting && <Loader2 className="size-4 animate-spin" />}
              {category ? "Save changes" : "Add category"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
