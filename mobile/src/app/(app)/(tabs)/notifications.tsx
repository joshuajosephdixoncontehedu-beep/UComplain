import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, RefreshControl, ScrollView, Text, View } from 'react-native';

import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { useScreenTopOffset } from '@/hooks/use-screen-top-offset';
import { NotificationItem, NotificationType, notificationsApi } from '@/lib/api/notifications';

const TYPE_ICON: Record<NotificationType, keyof typeof Ionicons.glyphMap> = {
  ClarificationRequested: 'help-circle-outline',
  ReportVerified: 'checkmark-circle-outline',
  AssignmentMade: 'person-outline',
  WorkStarted: 'construct-outline',
  ReportResolved: 'checkmark-done-outline',
  ReportRejected: 'close-circle-outline',
  ReportClosedDuplicate: 'archive-outline',
  ReportAutoClosed: 'time-outline',
};

function relativeTime(iso: string): string {
  const minutes = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h`;
  return `${Math.floor(hours / 24)}d`;
}

function isToday(iso: string) {
  const d = new Date(iso);
  const now = new Date();
  return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth() && d.getDate() === now.getDate();
}

function NotificationRow({ item, onPress }: { item: NotificationItem; onPress: () => void }) {
  return (
    <Pressable onPress={onPress} className="flex-row items-start gap-3.5 px-5 py-3.5 active:bg-surface-muted">
      <View className="h-[38px] w-[38px] items-center justify-center rounded-full bg-brand-tint">
        <Ionicons name={TYPE_ICON[item.type]} size={19} color="#1D4ED8" />
      </View>
      <View className="flex-1">
        <View className="flex-row items-center justify-between">
          <Text className="text-body-lg font-semibold text-ink">{item.title}</Text>
          <Text className="text-caption text-muted">{relativeTime(item.createdAt)}</Text>
        </View>
        <Text className="mt-1 text-body-sm text-muted">{item.body}</Text>
      </View>
      {!item.readAt ? <View className="mt-1.5 h-2 w-2 rounded-full bg-brand" /> : null}
    </Pressable>
  );
}

/** Figma "18 Notifications" (node 17:172), reached via Home's bell icon. */
export default function Notifications() {
  const { authorizedRequest } = useReporterAuth();
  const topOffset = useScreenTopOffset();
  const [items, setItems] = useState<NotificationItem[] | null>(null);
  const [error, setError] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const load = () =>
    notificationsApi
      .getMine(authorizedRequest, { page: 1, pageSize: 50 })
      .then((res) => {
        setItems(res.items);
        setError(false);
      })
      .catch(() => setError(true));

  useEffect(() => {
    load();
    // Fetch once on mount; pull-to-refresh re-triggers explicitly.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onPressItem = async (item: NotificationItem) => {
    if (!item.readAt) {
      setItems((prev) => prev?.map((n) => (n.id === item.id ? { ...n, readAt: new Date().toISOString() } : n)) ?? prev);
      notificationsApi.markRead(authorizedRequest, item.id).catch(() => undefined);
    }
    if (item.reportId) router.push(`/(app)/(tabs)/my-reports/${item.reportId}`);
  };

  const onMarkAllRead = async () => {
    setItems((prev) => prev?.map((n) => ({ ...n, readAt: n.readAt ?? new Date().toISOString() })) ?? prev);
    await notificationsApi.markAllRead(authorizedRequest).catch(() => undefined);
  };

  const today = items?.filter((n) => isToday(n.createdAt)) ?? [];
  const earlier = items?.filter((n) => !isToday(n.createdAt)) ?? [];
  const hasUnread = items?.some((n) => !n.readAt) ?? false;

  return (
    <ScrollView
      className="flex-1 bg-canvas"
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
      <View className="flex-row items-center justify-between px-5" style={{ marginTop: topOffset }}>
        <Text className="text-h1 text-ink">Notifications</Text>
        {hasUnread ? (
          <Pressable hitSlop={8} onPress={onMarkAllRead}>
            <Text className="text-body-sm font-semibold text-brand">Mark all read</Text>
          </Pressable>
        ) : null}
      </View>

      {error ? (
        <Text className="mt-9 px-5 text-body text-status-critical">Could not load notifications. Pull down to try again.</Text>
      ) : !items ? (
        <View className="mt-16 items-center">
          <ActivityIndicator color="#1D4ED8" />
        </View>
      ) : items.length === 0 ? (
        <Text className="mt-9 px-5 text-body text-muted">No notifications yet.</Text>
      ) : (
        <>
          {today.length > 0 ? (
            <>
              <Text className="mb-1 mt-9 px-5 text-eyebrow uppercase tracking-wide text-muted">Today</Text>
              {today.map((item) => (
                <NotificationRow key={item.id} item={item} onPress={() => onPressItem(item)} />
              ))}
            </>
          ) : null}

          {earlier.length > 0 ? (
            <>
              <Text className="mb-1 mt-4 px-5 text-eyebrow uppercase tracking-wide text-muted">Earlier</Text>
              {earlier.map((item) => (
                <NotificationRow key={item.id} item={item} onPress={() => onPressItem(item)} />
              ))}
            </>
          ) : null}
        </>
      )}

      <View className="h-24" />
    </ScrollView>
  );
}
