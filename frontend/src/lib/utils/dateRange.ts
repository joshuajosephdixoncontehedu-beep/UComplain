import { format, subDays } from "date-fns";

export type DateRangePreset = "today" | "7d" | "30d" | "custom";

export interface DateRangeValue {
  preset: DateRangePreset;
  from: string;
  to: string;
}

const ISO_DATE = "yyyy-MM-dd";

export function presetToRange(preset: Exclude<DateRangePreset, "custom">): { from: string; to: string } {
  const today = new Date();
  const to = format(today, ISO_DATE);
  switch (preset) {
    case "today":
      return { from: to, to };
    case "7d":
      return { from: format(subDays(today, 6), ISO_DATE), to };
    case "30d":
      return { from: format(subDays(today, 29), ISO_DATE), to };
  }
}

export function defaultDateRange(): DateRangeValue {
  return { preset: "30d", ...presetToRange("30d") };
}
