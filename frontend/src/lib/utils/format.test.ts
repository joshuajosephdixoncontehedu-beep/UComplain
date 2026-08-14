import { describe, expect, it } from "vitest";
import { formatHours, formatPercentChange } from "./format";

describe("formatPercentChange", () => {
  it("reports 'New' when there was no prior-period value to compare against", () => {
    expect(formatPercentChange(5, 0)).toEqual({ label: "New", direction: "up" });
  });

  it("reports 'No change' when both periods are zero", () => {
    expect(formatPercentChange(0, 0)).toEqual({ label: "No change", direction: "flat" });
  });

  it("reports 'No change' for a sub-1% swing rather than a misleadingly precise number", () => {
    expect(formatPercentChange(100, 100.5)).toEqual({ label: "No change", direction: "flat" });
  });

  it("computes a signed percentage for a real change", () => {
    expect(formatPercentChange(150, 100)).toEqual({ label: "+50%", direction: "up" });
    expect(formatPercentChange(50, 100)).toEqual({ label: "-50%", direction: "down" });
  });
});

describe("formatHours", () => {
  it("renders sub-hour durations in minutes", () => {
    expect(formatHours(0.5)).toBe("30 min");
  });

  it("renders multi-hour durations under two days in hours", () => {
    expect(formatHours(10.25)).toBe("10.3 hrs");
  });

  it("renders durations of two days or more in days", () => {
    expect(formatHours(72)).toBe("3.0 days");
  });

  it("renders a dash for missing data", () => {
    expect(formatHours(null)).toBe("—");
    expect(formatHours(undefined)).toBe("—");
  });
});
