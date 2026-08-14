import { AdminRole } from "@/types/enums";

/** Mirrors the backend's SuperAdminOnly policy. */
export function isSuperAdmin(role: AdminRole): boolean {
  return role === AdminRole.SuperAdmin;
}

/** Mirrors the backend's ManagerOrAbove policy. */
export function isManagerOrAbove(role: AdminRole): boolean {
  return role === AdminRole.SuperAdmin || role === AdminRole.IncidentManager;
}

/** Mirrors the backend's ReviewerOrAbove policy. */
export function isReviewerOrAbove(role: AdminRole): boolean {
  return isManagerOrAbove(role) || role === AdminRole.Reviewer;
}

export function isReadOnlyAnalyst(role: AdminRole): boolean {
  return role === AdminRole.ReadOnlyAnalyst;
}

export function roleLabel(role: AdminRole): string {
  switch (role) {
    case AdminRole.SuperAdmin:
      return "Super Admin";
    case AdminRole.IncidentManager:
      return "Incident Manager";
    case AdminRole.Reviewer:
      return "Reviewer";
    case AdminRole.ReadOnlyAnalyst:
      return "Read-Only Analyst";
  }
}

export function roleDescription(role: AdminRole): string {
  switch (role) {
    case AdminRole.SuperAdmin:
      return "Unrestricted access, including administrator management, settings, and audit logs.";
    case AdminRole.IncidentManager:
      return "Manages reports, verification, categories, analytics, and assignments.";
    case AdminRole.Reviewer:
      return "Reviews reports and the verification queue; can approve, reject, request clarification, and add notes.";
    case AdminRole.ReadOnlyAnalyst:
      return "Read-only access to the dashboard, reports, and analytics.";
  }
}
