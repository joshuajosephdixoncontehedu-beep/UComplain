import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { verificationAge } from "./verificationAge";

describe("verificationAge", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("reports 'Within SLA' well before the deadline", () => {
    vi.setSystemTime(new Date("2026-01-01T12:00:00Z"));
    const result = verificationAge("2026-01-01T10:00:00Z", 24);
    expect(result.hoursElapsed).toBe(2);
    expect(result.tone).toBe("slate");
    expect(result.label).toBe("Within SLA");
  });

  it("reports 'Due soon' once past 75% of the SLA window", () => {
    vi.setSystemTime(new Date("2026-01-01T12:00:00Z"));
    const dueSoon = verificationAge("2026-01-01T00:00:00Z", 15); // 12h elapsed of a 15h SLA = 80%
    expect(dueSoon.tone).toBe("amber");
    expect(dueSoon.label).toBe("Due soon");
  });

  it("reports 'Overdue' once the SLA window has fully elapsed", () => {
    vi.setSystemTime(new Date("2026-01-02T12:00:00Z"));
    const result = verificationAge("2026-01-01T00:00:00Z", 24);
    expect(result.tone).toBe("red");
    expect(result.label).toBe("Overdue");
  });
});
