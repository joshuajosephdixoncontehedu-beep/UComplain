import type { DashboardMetrics, NamedCount, TimeSeriesPoint } from "./dashboard";

export interface AssignmentWorkload {
  adminId: string;
  adminName: string;
  openAssignedCount: number;
  resolvedInRangeCount: number;
}

export interface CategoryResponseTime {
  categoryName: string;
  averageResolutionTimeHours: number | null;
}

export interface AnalyticsResponse {
  from: string;
  to: string;
  metrics: DashboardMetrics;
  reportVolumeOverTime: TimeSeriesPoint[];
  categoryDistribution: NamedCount[];
  statusDistribution: NamedCount[];
  verificationOutcomeDistribution: NamedCount[];
  assignmentWorkload: AssignmentWorkload[];
  resolutionTimeByCategory: CategoryResponseTime[];
}
