import { apiRequest } from '@/lib/api/client';

export type PublicIncidentAgeBucket = 'Today' | 'ThisWeek' | 'ThisMonth' | 'Older';

export type PublicIncident = {
  id: string;
  categoryName: string;
  categoryIconKey: string | null;
  categoryColourToken: string | null;
  latitude: number;
  longitude: number;
  distanceMeters: number;
  ageBucket: PublicIncidentAgeBucket;
};

/** The one entirely anonymous mobile surface (PublicMapController — no reporter token). */
export const publicMapApi = {
  getNearby: (lat: number, lng: number, radiusM?: number) =>
    apiRequest<PublicIncident[]>('api/mobile/public/incidents', { query: { lat, lng, radiusM } }),
};
