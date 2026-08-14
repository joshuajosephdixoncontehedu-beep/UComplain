import { apiGet, apiPost } from "./client";
import type { AdminProfile, AuthTokenResponse } from "@/types/auth";

export function login(email: string, password: string) {
  return apiPost<AuthTokenResponse>("/api/admin/auth/login", { email, password }, { skipAuthRefresh: true });
}

export function refresh(refreshToken: string) {
  return apiPost<AuthTokenResponse>("/api/admin/auth/refresh", { refreshToken }, { skipAuthRefresh: true });
}

export function logout(refreshToken: string) {
  return apiPost<void>("/api/admin/auth/logout", { refreshToken });
}

export function me() {
  return apiGet<AdminProfile>("/api/admin/auth/me");
}
