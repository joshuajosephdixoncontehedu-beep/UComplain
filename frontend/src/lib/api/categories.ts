import { apiGet, apiPatch, apiPost } from "./client";
import type { Category, CreateCategoryRequest, UpdateCategoryRequest } from "@/types/categories";

export function getCategories() {
  return apiGet<Category[]>("/api/admin/categories");
}

export function createCategory(request: CreateCategoryRequest) {
  return apiPost<Category>("/api/admin/categories", request);
}

export function updateCategory(id: string, request: UpdateCategoryRequest) {
  return apiPatch<Category>(`/api/admin/categories/${id}`, request);
}

export function disableCategory(id: string) {
  return apiPost<Category>(`/api/admin/categories/${id}/disable`);
}
