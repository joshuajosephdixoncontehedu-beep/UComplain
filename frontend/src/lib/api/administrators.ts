import { apiGet, apiPatch, apiPost } from "./client";
import type { Administrator, CreateAdministratorRequest, UpdateAdministratorRequest } from "@/types/administrators";

export function getAdministrators() {
  return apiGet<Administrator[]>("/api/admin/administrators");
}

export function getAdministrator(id: string) {
  return apiGet<Administrator>(`/api/admin/administrators/${id}`);
}

export function createAdministrator(request: CreateAdministratorRequest) {
  return apiPost<Administrator>("/api/admin/administrators", request);
}

export function updateAdministrator(id: string, request: UpdateAdministratorRequest) {
  return apiPatch<Administrator>(`/api/admin/administrators/${id}`, request);
}

export function deactivateAdministrator(id: string) {
  return apiPost<Administrator>(`/api/admin/administrators/${id}/deactivate`);
}

export function reactivateAdministrator(id: string) {
  return apiPost<Administrator>(`/api/admin/administrators/${id}/reactivate`);
}
