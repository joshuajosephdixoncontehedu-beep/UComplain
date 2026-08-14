"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { presetToRange, type DateRangePreset, type DateRangeValue } from "@/lib/utils/dateRange";

const PRESETS: { key: Exclude<DateRangePreset, "custom">; label: string }[] = [
  { key: "today", label: "Today" },
  { key: "7d", label: "7 days" },
  { key: "30d", label: "30 days" },
];

export function DateRangeControl({
  value,
  onChange,
}: {
  value: DateRangeValue;
  onChange: (value: DateRangeValue) => void;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2" role="group" aria-label="Date range">
      {PRESETS.map((preset) => (
        <Button
          key={preset.key}
          type="button"
          size="sm"
          variant={value.preset === preset.key ? "default" : "outline"}
          onClick={() => onChange({ preset: preset.key, ...presetToRange(preset.key) })}
        >
          {preset.label}
        </Button>
      ))}
      <Button
        type="button"
        size="sm"
        variant={value.preset === "custom" ? "default" : "outline"}
        onClick={() => onChange({ ...value, preset: "custom" })}
      >
        Custom
      </Button>

      {value.preset === "custom" && (
        <div className="flex items-center gap-1.5 pl-1">
          <Input
            type="date"
            aria-label="From date"
            value={value.from}
            max={value.to}
            onChange={(e) => onChange({ ...value, preset: "custom", from: e.target.value })}
            className={cn("h-8 w-[9.5rem]")}
          />
          <span className="text-sm text-muted-foreground">to</span>
          <Input
            type="date"
            aria-label="To date"
            value={value.to}
            min={value.from}
            onChange={(e) => onChange({ ...value, preset: "custom", to: e.target.value })}
            className="h-8 w-[9.5rem]"
          />
        </div>
      )}
    </div>
  );
}
