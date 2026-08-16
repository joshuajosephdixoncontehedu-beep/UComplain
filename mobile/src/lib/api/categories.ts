// Mirrors backend/.../MobileReports/Dtos/MobileCategoryDto.cs
export type MobileCategory = {
  id: string;
  name: string;
  slug: string | null;
  iconKey: string | null;
  colourToken: string | null;
  defaultPriority: 'Low' | 'Medium' | 'High' | 'Critical';
  displayOrder: number;
};

type AuthorizedRequest = <T>(path: string) => Promise<T>;

export const categoriesApi = {
  getActive: (authorizedRequest: AuthorizedRequest) => authorizedRequest<MobileCategory[]>('api/mobile/categories'),
};
