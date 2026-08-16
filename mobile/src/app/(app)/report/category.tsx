import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';

import { AppButton } from '@/components/ui/button';
import { useReportDraft } from '@/components/report/report-draft-context';
import { WizardHeader } from '@/components/report/wizard-header';
import { iconForCategory, useCategories } from '@/lib/use-categories';

/** Figma "08 New report · Category" (node 8:160) — wizard step 1 of 4. */
export default function ReportCategoryScreen() {
  const { category: presetSlug } = useLocalSearchParams<{ category?: string }>();
  const { draft, sync, syncing, syncError } = useReportDraft();
  const { categories, error } = useCategories();
  const [selectedId, setSelectedId] = useState<string | undefined>(
    draft.category?.id ?? categories?.find((c) => c.slug === presetSlug)?.id,
  );

  return (
    <View className="flex-1 bg-canvas px-5">
      <WizardHeader step={1} onBack={() => router.replace('/(app)/(tabs)/home')} />

      <View className="mt-8 gap-2">
        <Text className="text-h1 text-ink">What kind of incident?</Text>
        <Text className="text-body text-secondary">Choose the closest match. Responders use this to route your report.</Text>
      </View>

      {error ? (
        <Text className="mt-9 text-body text-status-critical">Could not load categories. Pull down to try again, or check your connection.</Text>
      ) : !categories ? (
        <View className="mt-16 items-center">
          <ActivityIndicator color="#1D4ED8" />
        </View>
      ) : categories.length === 0 ? (
        <Text className="mt-9 text-body text-muted">No categories are available yet.</Text>
      ) : (
        <ScrollView className="mt-9" contentContainerClassName="flex-row flex-wrap justify-between gap-y-3 pb-6">
          {categories
            .slice()
            .sort((a, b) => a.displayOrder - b.displayOrder)
            .map((cat) => {
              const isSelected = cat.id === selectedId;
              const icon = iconForCategory(cat.slug, cat.iconKey);
              return (
                <Pressable
                  key={cat.id}
                  onPress={() => setSelectedId(cat.id)}
                  className={`w-[169px] items-start gap-4 rounded-card border p-4 ${
                    isSelected ? 'border-brand bg-brand-tint' : 'border-border bg-surface'
                  }`}>
                  <View className={`h-[38px] w-[38px] items-center justify-center rounded-full ${isSelected ? 'bg-surface' : 'bg-surface-muted'}`}>
                    <Ionicons name={icon} size={20} color={isSelected ? '#1D4ED8' : '#334155'} />
                  </View>
                  <Text className={`text-body ${isSelected ? 'font-semibold text-brand' : 'text-ink'}`}>{cat.name}</Text>
                </Pressable>
              );
            })}
        </ScrollView>
      )}

      {syncError ? <Text className="mt-3 text-body-sm text-status-critical">{syncError}</Text> : null}

      <View className="mb-10 mt-2">
        <AppButton
          title={syncing ? 'Saving…' : 'Continue'}
          disabled={!selectedId || syncing}
          onPress={async () => {
            const category = categories?.find((c) => c.id === selectedId);
            if (!category) return;
            try {
              await sync({
                category: { id: category.id, slug: category.slug, label: category.name, icon: iconForCategory(category.slug, category.iconKey) },
              });
              router.push('/(app)/report/details');
            } catch {
              // syncError already set by the context; stay on this screen.
            }
          }}
        />
      </View>
    </View>
  );
}
