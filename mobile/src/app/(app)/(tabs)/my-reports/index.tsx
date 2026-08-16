import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';

import { ReportCard } from '@/components/ui/report-card';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { MobileReportListItem, ReportCounts, ReportListBucket, reportsApi } from '@/lib/api/reports';
import { useCategories } from '@/lib/use-categories';

type FilterKey = 'all' | ReportListBucket;
const FILTERS: { key: FilterKey; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'Active', label: 'Active' },
  { key: 'Resolved', label: 'Resolved' },
  { key: 'Rejected', label: 'Rejected' },
];

function relativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

/** Figma "14 My reports" (node 14:3). */
export default function MyReports() {
  const { authorizedRequest } = useReporterAuth();
  const { iconByName } = useCategories();
  const topOffset = useScreenTopOffset();
  const [filter, setFilter] = useState<FilterKey>('all');
  const [result, setResult] = useState<{ filter: FilterKey; reports: MobileReportListItem[] | null; error: boolean }>({
    filter: 'all',
    reports: null,
    error: false,
  });

  useEffect(() => {
    reportsApi
      .getMyReports(authorizedRequest, { page: 1, pageSize: 50, status: filter === 'all' ? undefined : filter })
      .then((res) => setResult({ filter, reports: res.items, error: false }))
      .catch(() => setResult({ filter, reports: null, error: true }));
  }, [filter, authorizedRequest]);

  // While a newer filter's request is in flight, treat the stale result as still-loading.
  const reports = result.filter === filter ? result.reports : null;
  const error = result.filter === filter && result.error;

  const [counts, setCounts] = useState<ReportCounts | null>(null);
  useEffect(() => {
    reportsApi
      .getCounts(authorizedRequest)
      .then(setCounts)
      .catch(() => undefined);
    // Fetch once on mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const countFor = (key: FilterKey) => {
    if (!counts) return undefined;
    if (key === 'all') return counts.total;
    if (key === 'Active') return counts.active;
    if (key === 'Resolved') return counts.resolved;
    return counts.rejected;
  };

  return (
    <View className="flex-1 bg-canvas">
      <View className="flex-row items-center justify-between px-5" style={{ marginTop: topOffset }}>
        <Text className="text-h1 text-ink">My reports</Text>
        <Pressable hitSlop={8} className="h-[42px] w-[42px] items-center justify-center rounded-full bg-surface-muted">
          <Ionicons name="options-outline" size={19} color="#0F172A" />
        </Pressable>
      </View>

      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        style={{ marginTop: 26 }}
        contentContainerClassName="items-center gap-2 px-5">
        {FILTERS.map((f) => {
          const active = f.key === filter;
          const count = countFor(f.key);
          return (
            <Pressable
              key={f.key}
              onPress={() => setFilter(f.key)}
              className={`flex-row items-center gap-2 self-center rounded-pill px-3.5 py-2 ${active ? 'bg-brand' : 'bg-surface-muted'}`}>
              <Text className={`text-label ${active ? 'text-surface' : 'text-secondary'}`}>{f.label}</Text>
              {count !== undefined ? (
                <View className={`h-4 min-w-[19px] items-center justify-center rounded-pill px-1 ${active ? 'bg-white/25' : 'bg-surface'}`}>
                  <Text className={`text-[11px] font-semibold ${active ? 'text-surface' : 'text-secondary'}`}>{count}</Text>
                </View>
              ) : null}
            </Pressable>
          );
        })}
      </ScrollView>

      {error ? (
        <Text className="mt-9 px-5 text-body text-status-critical">Could not load your reports. Pull down to try again.</Text>
      ) : !reports ? (
        <View className="mt-16 items-center">
          <ActivityIndicator color="#1D4ED8" />
        </View>
      ) : reports.length === 0 ? (
        <Text className="mt-9 px-5 text-body text-muted">No reports in this filter yet.</Text>
      ) : (
        <ScrollView className="flex-1 px-5" style={{ marginTop: 26 }} contentContainerClassName="gap-3 pb-32">
          {reports.map((r) => (
            <ReportCard
              key={r.id}
              icon={iconByName(r.categoryName)}
              title={r.categoryName}
              status={r.statusBadge}
              caseReference={r.caseReference}
              relativeTime={relativeTime(r.createdAt)}
              progress={r.progressPercent / 100}
              onPress={() => router.push(`/(app)/(tabs)/my-reports/${r.id}`)}
            />
          ))}
        </ScrollView>
      )}
    </View>
  );
}
