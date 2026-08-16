import { AuthorizedRequestOptions } from '@/components/auth/reporter-auth-context';
import { ReporterProfile } from '@/lib/api/auth';

export type ReporterPrivacySetting = {
  usePreciseLocation: boolean;
  showOnPublicMap: boolean;
  allowResponderContact: boolean;
  updatedAt: string;
};

export type ReporterStats = {
  activeReports: number;
  resolvedReports: number;
  rejectedReports: number;
  totalReports: number;
  memberSince: string;
};

export type DataExportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';
export type DataExportRequest = {
  id: string;
  status: DataExportStatus;
  requestedAt: string;
  completedAt: string | null;
  downloadUrl: string | null;
  downloadUrlExpiresAt: string | null;
  failureReason: string | null;
};

export type AccountDeletionStatus = 'Pending' | 'Cancelled' | 'Completed';
export type AccountDeletionRequest = {
  id: string;
  status: AccountDeletionStatus;
  requestedAt: string;
  scheduledForAt: string;
  cancelledAt: string | null;
};

type AuthorizedRequest = <T>(path: string, options?: AuthorizedRequestOptions) => Promise<T>;

export const meApi = {
  getPrivacy: (req: AuthorizedRequest) => req<ReporterPrivacySetting>('api/mobile/me/privacy'),

  updatePrivacy: (req: AuthorizedRequest, input: { usePreciseLocation: boolean; showOnPublicMap: boolean; allowResponderContact: boolean }) =>
    req<ReporterPrivacySetting>('api/mobile/me/privacy', { method: 'PUT', body: input }),

  getStats: (req: AuthorizedRequest) => req<ReporterStats>('api/mobile/me/stats'),

  updateProfile: (req: AuthorizedRequest, input: { fullName: string; languagePreference?: string | null }) =>
    req<ReporterProfile>('api/mobile/me', { method: 'PATCH', body: input }),

  requestDataExport: (req: AuthorizedRequest) => req<DataExportRequest>('api/mobile/me/data-export', { method: 'POST' }),

  getLatestDataExport: (req: AuthorizedRequest) => req<DataExportRequest>('api/mobile/me/data-export'),

  requestAccountDeletion: (req: AuthorizedRequest) => req<AccountDeletionRequest>('api/mobile/me', { method: 'DELETE' }),

  cancelAccountDeletion: (req: AuthorizedRequest) => req<AccountDeletionRequest>('api/mobile/me/deletion/cancel', { method: 'POST' }),
};
