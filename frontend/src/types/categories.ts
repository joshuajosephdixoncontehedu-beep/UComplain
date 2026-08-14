import type { IncidentPriority } from "./enums";

export interface Category {
  id: string;
  name: string;
  description: string;
  defaultPriority: IncidentPriority;
  slaHours: number;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCategoryRequest {
  name: string;
  description: string;
  defaultPriority: IncidentPriority;
  slaHours: number;
  displayOrder: number;
}

export type UpdateCategoryRequest = CreateCategoryRequest;
