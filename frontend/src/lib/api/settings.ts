import { apiGet, apiPatch } from "./client";
import type { Settings, UpdateSettingsRequest } from "@/types/settings";

export function getSettings() {
  return apiGet<Settings>("/api/admin/settings");
}

export function updateSettings(request: UpdateSettingsRequest) {
  return apiPatch<Settings>("/api/admin/settings", request);
}
