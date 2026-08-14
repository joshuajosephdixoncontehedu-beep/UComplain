import type { AdminRole } from "./enums";

export interface Administrator {
  id: string;
  fullName: string;
  email: string;
  role: AdminRole;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface CreateAdministratorRequest {
  fullName: string;
  email: string;
  role: AdminRole;
  temporaryPassword: string;
}

export interface UpdateAdministratorRequest {
  fullName: string;
  email: string;
  role: AdminRole;
}
