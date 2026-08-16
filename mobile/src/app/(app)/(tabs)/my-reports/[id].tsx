import { Ionicons } from '@expo/vector-icons';
import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, TextInput, View } from 'react-native';

import { BackButton } from '@/components/ui/back-button';
import { StatusBadge } from '@/components/ui/status-badge';
import { TimelineStep } from '@/components/report/timeline-step';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { CaseStatus, ClarificationRequest, MobileReportDetail, reportsApi } from '@/lib/api/reports';
import { caseStatusLabel, projectReportStatus } from '@/lib/report-status-projection';
import { useCategories } from '@/lib/use-categories';

const CASE_STATUS_ICON: Record<CaseStatus, keyof typeof Ionicons.glyphMap> = {
  VerificationPending: 'document-text-outline',
  UnderReview: 'eye-outline',
  Assigned: 'person-outline',
  InProgress: 'construct-outline',
  Resolved: 'checkmark-done-outline',
  Closed: 'checkmark-done-outline',
  Rejected: 'close-circle-outline',
  Duplicate: 'copy-outline',
  Withdrawn: 'arrow-undo-outline',
};

function formatDateTime(iso: string) {
  const d = new Date(iso);
  return `${d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' })}, ${d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false })}`;
}
function relativeTime(iso: string): string {
  const minutes = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
  if (minutes < 60) return `${Math.max(minutes, 0)}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

/** Renders the report's real chronological CaseStatus transitions, plus a synthetic final step
 *  for the current badge when it reflects a VerificationStatus change not captured by statusHistory
 *  (e.g. "Needs clarification" while caseStatus is still VerificationPending). */
function timelineSteps(report: MobileReportDetail, currentBadgeLabel: string) {
  const steps: { icon: keyof typeof Ionicons.glyphMap; title: string; detail: string }[] = [
    { icon: 'document-text-outline', title: 'Report received', detail: formatDateTime(report.createdAt) },
    ...report.statusHistory.map((h) => ({
      icon: CASE_STATUS_ICON[h.newStatus],
      title: caseStatusLabel(h.newStatus),
      detail: formatDateTime(h.createdAt),
    })),
  ];
  if (steps[steps.length - 1].title !== currentBadgeLabel) {
    steps.push({
      icon: CASE_STATUS_ICON[report.caseStatus],
      title: currentBadgeLabel,
      detail: `Updated ${relativeTime(report.updatedAt)}`,
    });
  }
  return steps;
}

/** Figma "15 Report detail" (node 14:171). */
export default function ReportDetail() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { authorizedRequest } = useReporterAuth();
  const { iconByName } = useCategories();
  const topOffset = useScreenTopOffset();

  const [report, setReport] = useState<MobileReportDetail | null>(null);
  const [clarifications, setClarifications] = useState<ClarificationRequest[] | null>(null);
  const [error, setError] = useState(false);
  const [withdrawing, setWithdrawing] = useState(false);
  const [withdrawReason, setWithdrawReason] = useState('');
  const [submittingWithdraw, setSubmittingWithdraw] = useState(false);

  useEffect(() => {
    Promise.all([reportsApi.getById(authorizedRequest, id), reportsApi.getClarifications(authorizedRequest, id)])
      .then(([r, c]) => {
        setReport(r);
        setClarifications(c);
      })
      .catch(() => setError(true));
    // Fetch once per report id.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  if (error) {
    return (
      <View className="flex-1 items-center justify-center bg-canvas px-5">
        <Text className="text-body text-status-critical">Could not load this report.</Text>
      </View>
    );
  }
  if (!report) {
    return (
      <View className="flex-1 items-center justify-center bg-canvas">
        <ActivityIndicator color="#1D4ED8" />
      </View>
    );
  }

  const projection = projectReportStatus(report.verificationStatus, report.caseStatus);
  const pendingClarification = clarifications?.find((c) => !c.resolvedAt && !c.autoClosedAt);

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="pb-10">
      <View className="flex-row items-center justify-between px-5" style={{ marginTop: topOffset }}>
        <View className="flex-row items-center gap-5">
          <BackButton />
          <Text className="text-body-lg font-semibold text-ink">{report.caseReference}</Text>
        </View>
      </View>

      <View className="mt-[60px] gap-3 px-5">
        <View className="flex-row items-center gap-2">
          <StatusBadge variant={projection.badgeLabel} />
          <StatusBadge variant={report.priority} />
          <Text className="ml-auto text-body-sm text-muted">Updated {relativeTime(report.updatedAt)}</Text>
        </View>
        <Text className="text-h1 text-ink">{report.description}</Text>
      </View>

      {pendingClarification ? (
        <Pressable
          onPress={() => router.push({ pathname: '/(app)/(tabs)/my-reports/[id]/clarification', params: { id, clarificationId: pendingClarification.id } })}
          className="mx-5 mt-6 flex-row items-center gap-3 rounded-card bg-status-pending-tint p-3.5">
          <Ionicons name="information-circle-outline" size={20} color="#B45309" />
          <Text className="flex-1 text-body-sm text-status-pending">More information needed — tap to reply</Text>
          <Ionicons name="chevron-forward" size={16} color="#B45309" />
        </Pressable>
      ) : null}

      {report.caseStatus === 'Withdrawn' ? (
        <View className="mx-5 mt-6 rounded-card bg-surface-muted p-3.5">
          <Text className="text-body-sm text-secondary">
            Withdrawn{report.withdrawalReason ? `: ${report.withdrawalReason}` : ''}
          </Text>
        </View>
      ) : (
        <>
          <Text className="mb-4 mt-9 px-5 text-eyebrow uppercase tracking-wide text-muted">Progress</Text>
          <View className="px-5">
            {timelineSteps(report, projection.badgeLabel).map((step, i, all) => (
              <TimelineStep key={i} icon={step.icon} title={step.title} detail={step.detail} done isLast={i === all.length - 1} />
            ))}
          </View>
        </>
      )}

      <View className="mx-5 mt-3 gap-4 rounded-card border border-border bg-surface p-4">
        <View className="flex-row items-center gap-3">
          <View className="h-9 w-9 items-center justify-center rounded-full bg-brand-tint">
            <Ionicons name={iconByName(report.categoryName)} size={18} color="#1D4ED8" />
          </View>
          <View>
            <Text className="text-body font-semibold text-ink">{report.categoryName}</Text>
            <Text className="text-body-sm text-muted">
              {report.locationDescription}
              {report.landmark ? ` · ${report.landmark}` : ''}
            </Text>
          </View>
        </View>

        {report.statusHistory.length > 0 ? (
          <>
            <View className="h-px bg-border" />
            <Text className="text-body-sm text-secondary">
              {caseStatusLabel(report.statusHistory[report.statusHistory.length - 1].newStatus)} on{' '}
              {formatDateTime(report.statusHistory[report.statusHistory.length - 1].createdAt)}
            </Text>
          </>
        ) : null}
      </View>

      {report.caseStatus !== 'Withdrawn' && (
        <View className="mt-9 px-5">
          {withdrawing ? (
            <View className="gap-3 rounded-card border border-border bg-surface p-4">
              <Text className="text-body-sm font-semibold text-ink">Why are you withdrawing this report?</Text>
              <TextInput
                className="h-12 rounded-input border border-border bg-canvas px-3.5 text-body text-ink"
                placeholder="Reason"
                placeholderTextColor="#94A3B8"
                value={withdrawReason}
                onChangeText={setWithdrawReason}
              />
              <View className="flex-row gap-3">
                <Pressable onPress={() => setWithdrawing(false)} className="h-11 flex-1 items-center justify-center rounded-input border border-border">
                  <Text className="text-body-sm font-semibold text-ink">Cancel</Text>
                </Pressable>
                <Pressable
                  onPress={async () => {
                    setSubmittingWithdraw(true);
                    try {
                      const updated = await reportsApi.withdraw(authorizedRequest, id, withdrawReason || 'No reason given');
                      setReport(updated);
                      setWithdrawing(false);
                    } finally {
                      setSubmittingWithdraw(false);
                    }
                  }}
                  disabled={!withdrawReason.trim() || submittingWithdraw}
                  className="h-11 flex-1 items-center justify-center rounded-input bg-status-critical disabled:opacity-50">
                  <Text className="text-body-sm font-semibold text-surface">{submittingWithdraw ? 'Withdrawing…' : 'Confirm withdrawal'}</Text>
                </Pressable>
              </View>
            </View>
          ) : (
            <View className="flex-row gap-3">
              <Pressable
                onPress={() => router.push({ pathname: '/(app)/(tabs)/my-reports/[id]/clarification', params: { id } })}
                className="h-[50px] flex-1 flex-row items-center justify-center gap-2 rounded-input border border-border bg-surface">
                <Ionicons name="chatbubble-outline" size={18} color="#334155" />
                <Text className="text-body font-semibold text-ink">Add information</Text>
              </Pressable>
              <Pressable onPress={() => setWithdrawing(true)} className="h-[50px] flex-1 items-center justify-center rounded-input border border-border bg-surface">
                <Text className="text-body font-semibold text-status-critical">Withdraw</Text>
              </Pressable>
            </View>
          )}
        </View>
      )}
    </ScrollView>
  );
}
