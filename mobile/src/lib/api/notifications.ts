import { AuthorizedRequestOptions } from '@/components/auth/reporter-auth-context';

export type NotificationType =
  | 'ClarificationRequested'
  | 'ReportVerified'
  | 'AssignmentMade'
  | 'WorkStarted'
  | 'ReportResolved'
  | 'ReportRejected'
  | 'ReportClosedDuplicate'
  | 'ReportAutoClosed';

export type NotificationItem = {
  id: string;
  type: NotificationType;
  title: string;
  body: string;
  reportId: string | null;
  readAt: string | null;
  createdAt: string;
};

type AuthorizedRequest = <T>(path: string, options?: AuthorizedRequestOptions) => Promise<T>;

export const notificationsApi = {
  getMine: (req: AuthorizedRequest, query: { page?: number; pageSize?: number }) =>
    req<{ items: NotificationItem[]; total: number; page: number; pageSize: number }>('api/mobile/notifications', { query }),

  markAllRead: (req: AuthorizedRequest) => req<{ updatedCount: number }>('api/mobile/notifications/read-all', { method: 'POST' }),

  markRead: (req: AuthorizedRequest, id: string) => req<NotificationItem>(`api/mobile/notifications/${id}/read`, { method: 'POST' }),
};
