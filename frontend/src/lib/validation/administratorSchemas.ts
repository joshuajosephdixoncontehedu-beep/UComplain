import { z } from "zod";
import { AdminRole } from "@/types/enums";

export const createAdministratorSchema = z.object({
  fullName: z.string().trim().min(2, "Name must be at least 2 characters").max(100),
  email: z.string().trim().min(1, "Email is required").email("Enter a valid email address"),
  role: z.enum(AdminRole),
  temporaryPassword: z.string().min(8, "Temporary password must be at least 8 characters"),
});
export type CreateAdministratorFormValues = z.infer<typeof createAdministratorSchema>;

export const updateAdministratorSchema = createAdministratorSchema.omit({ temporaryPassword: true });
export type UpdateAdministratorFormValues = z.infer<typeof updateAdministratorSchema>;
