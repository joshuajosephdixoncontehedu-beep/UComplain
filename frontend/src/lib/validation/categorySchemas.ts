import { z } from "zod";
import { IncidentPriority } from "@/types/enums";

export const categoryFormSchema = z.object({
  name: z.string().trim().min(2, "Name must be at least 2 characters").max(100),
  description: z.string().trim().min(3, "Description must be at least 3 characters").max(500),
  defaultPriority: z.enum(IncidentPriority),
  slaHours: z.coerce.number().int().min(1, "SLA must be at least 1 hour").max(8760),
  displayOrder: z.coerce.number().int().min(0, "Display order can't be negative"),
});
// z.coerce fields have a different input type (unknown, pre-coercion) than output
// type (number) — react-hook-form needs both to type the form correctly.
export type CategoryFormInput = z.input<typeof categoryFormSchema>;
export type CategoryFormValues = z.output<typeof categoryFormSchema>;
