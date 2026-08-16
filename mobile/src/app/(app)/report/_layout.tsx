import { Stack } from 'expo-router';

import { ReportDraftProvider } from '@/components/report/report-draft-context';

export default function ReportWizardLayout() {
  return (
    <ReportDraftProvider>
      <Stack screenOptions={{ headerShown: false }} />
    </ReportDraftProvider>
  );
}
