import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, Text, View } from 'react-native';

import { Avatar } from '@/components/ui/avatar';
import { ReportCard } from '@/components/ui/report-card';
import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { MobileReportListItem, reportsApi } from '@/lib/api/reports';
import { iconForCategory, useCategories } from '@/lib/use-categories';

const QUICK_CATEGORIES_SLUGS = ['drainage', 'road', 'power', 'crime'];

function relativeTime(iso: string): string {
  const diffMs = Date.now() - new Date(iso).getTime();
  const minutes = Math.floor(diffMs / 60000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

/** Figma "07 Home" (node 8:3). */
export default function Home() {
  const { reporter, authorizedRequest, photoUrl } = useReporterAuth();
  const { categories, iconByName } = useCategories();
  const reporterName = reporter?.fullName ?? '';
  const topOffset = useScreenTopOffset();

  const [recent, setRecent] = useState<MobileReportListItem[] | null>(null);
  const [loadError, setLoadError] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const load = () =>
    reportsApi
      .getMyReports(authorizedRequest, { page: 1, pageSize: 2 })
      .then((res) => {
        setRecent(res.items);
        setLoadError(false);
      })
      .catch(() => setLoadError(true));

  useEffect(() => {
    load();
    // Fetch once on mount; pull-to-refresh re-triggers explicitly.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const quickCategories = (categories ?? [])
    .filter((c) => c.slug && QUICK_CATEGORIES_SLUGS.includes(c.slug))
    .sort((a, b) => QUICK_CATEGORIES_SLUGS.indexOf(a.slug!) - QUICK_CATEGORIES_SLUGS.indexOf(b.slug!));

  return (
    <ScrollView
      className="flex-1 bg-canvas"
      contentContainerClassName="px-5 pb-32"
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={async () => {
            setRefreshing(true);
            await load();
            setRefreshing(false);
          }}
        />
      }>
      <View className="flex-row items-center gap-3" style={{ marginTop: topOffset }}>
        <Avatar name={reporterName} photoUrl={photoUrl} />
        <View className="flex-1">
          <Text className="text-body-sm text-muted">Good {new Date().getHours() < 12 ? 'morning' : new Date().getHours() < 18 ? 'afternoon' : 'evening'}</Text>
          <Text className="text-h2 text-ink">{reporterName}</Text>
        </View>
        <Pressable
          onPress={() => router.push('/(app)/(tabs)/notifications')}
          hitSlop={8}
          className="h-[42px] w-[42px] items-center justify-center rounded-full bg-surface-muted">
          <Ionicons name="notifications-outline" size={20} color="#0F172A" />
        </Pressable>
      </View>

      <Pressable
        onPress={() => router.push('/(app)/report/category')}
        className="mt-9 flex-row items-center gap-4 rounded-card bg-brand p-5 active:bg-brand-deep">
        <View className="h-12 w-12 items-center justify-center rounded-full bg-white/15">
          <Ionicons name="add-circle-outline" size={24} color="#FFFFFF" />
        </View>
        <View className="flex-1">
          <Text className="text-body-lg font-semibold text-surface">Report an incident</Text>
          <Text className="text-body-sm text-brand-tint">Takes about a minute</Text>
        </View>
        <Ionicons name="chevron-forward" size={20} color="#FFFFFF" />
      </Pressable>

      {quickCategories.length > 0 ? (
        <>
          <Text className="mb-3 mt-8 text-eyebrow uppercase tracking-wide text-muted">Report quickly</Text>
          <View className="flex-row justify-between">
            {quickCategories.map((c) => (
              <Pressable
                key={c.id}
                onPress={() => router.push({ pathname: '/(app)/report/category', params: { category: c.slug ?? undefined } })}
                className="w-20 items-center gap-2">
                <View className="h-9 w-9 items-center justify-center rounded-full bg-brand-tint">
                  <Ionicons name={iconForCategory(c.slug, c.iconKey)} size={19} color="#1D4ED8" />
                </View>
                <Text className="text-caption text-secondary" numberOfLines={1}>
                  {c.name}
                </Text>
              </Pressable>
            ))}
          </View>
        </>
      ) : null}

      <View className="mb-3 mt-8 flex-row items-center justify-between">
        <Text className="text-h2 text-ink">Your reports</Text>
        <Pressable onPress={() => router.push('/(app)/(tabs)/my-reports')} hitSlop={8}>
          <Text className="text-body-sm font-semibold text-brand">See all</Text>
        </Pressable>
      </View>

      {loadError ? (
        <Text className="text-body-sm text-status-critical">Could not load your reports.</Text>
      ) : !recent ? (
        <ActivityIndicator color="#1D4ED8" />
      ) : recent.length === 0 ? (
        <Text className="text-body-sm text-muted">You have not submitted any reports yet.</Text>
      ) : (
        <View className="gap-3">
          {recent.map((r) => (
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
        </View>
      )}
    </ScrollView>
  );
}
