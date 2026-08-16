import { AuthorizedRequestOptions } from '@/components/auth/reporter-auth-context';
import { ClarificationResponse } from '@/lib/api/reports';

type AuthorizedRequest = <T>(path: string, options?: AuthorizedRequestOptions) => Promise<T>;

export const clarificationsApi = {
  reply: (req: AuthorizedRequest, clarificationId: string, message: string, attachmentId?: string) =>
    req<ClarificationResponse>(`api/mobile/clarifications/${clarificationId}/reply`, {
      method: 'POST',
      body: { message, attachmentId: attachmentId ?? null },
    }),
};
