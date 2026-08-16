import { Ionicons } from '@expo/vector-icons';
import { useEffect, useState } from 'react';

import { useReporterAuth } from '@/components/auth/reporter-auth-context';
import { categoriesApi, MobileCategory } from '@/lib/api/categories';

// IconKey is free-text (IncidentCategory.IconKey, admin-set, no fixed enum). This maps
// the values used by the expected/seeded categories to a glyph, falling back to a
// generic icon for anything unrecognized — see docs/mobile-client-backend-extension.md
// Phase 1 ("the actual 8 category values ... still need to be entered via the admin UI").
const ICON_BY_KEY: Record<string, keyof typeof Ionicons.glyphMap> = {
  drainage: 'rainy-outline',
  road: 'car-outline',
  power: 'flash-outline',
  water: 'water-outline',
  crime: 'shield-outline',
  fire: 'flame-outline',
  medical: 'medkit-outline',
  waste: 'trash-outline',
};
export const FALLBACK_CATEGORY_ICON: keyof typeof Ionicons.glyphMap = 'alert-circle-outline';

export function iconForCategory(slug: string | null | undefined, iconKey: string | null | undefined): keyof typeof Ionicons.glyphMap {
  return (slug && ICON_BY_KEY[slug]) || (iconKey && ICON_BY_KEY[iconKey]) || FALLBACK_CATEGORY_ICON;
}

let cache: MobileCategory[] | null = null;

/** Fetches GET /api/mobile/categories once per app session and shares the result. */
export function useCategories() {
  const { authorizedRequest } = useReporterAuth();
  const [categories, setCategories] = useState<MobileCategory[] | null>(cache);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (cache) return;
    categoriesApi
      .getActive(authorizedRequest)
      .then((active) => {
        cache = active;
        setCategories(active);
      })
      .catch(() => setError(true));
    // Fetch once; authorizedRequest identity changes on refresh but we don't want to re-fetch.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const byId = (id: string) => categories?.find((c) => c.id === id);
  const iconById = (id: string) => iconForCategory(byId(id)?.slug, byId(id)?.iconKey);

  // MobileReportListItemDto/MobileReportDetailDto only carry CategoryName, not CategoryId.
  const byName = (name: string) => categories?.find((c) => c.name === name);
  const iconByName = (name: string) => iconForCategory(byName(name)?.slug, byName(name)?.iconKey);

  return { categories, error, byId, iconById, byName, iconByName };
}
