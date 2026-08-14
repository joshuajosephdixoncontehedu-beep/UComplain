import { describe, expect, it } from "vitest";
import { getAllowedNextStatuses } from "./caseStatusTransitions";
import { CaseStatus } from "@/types/enums";

describe("getAllowedNextStatuses", () => {
  it("mirrors the backend's AllowedTransitions map for working states", () => {
    expect(getAllowedNextStatuses(CaseStatus.UnderReview)).toEqual([CaseStatus.Assigned, CaseStatus.InProgress]);
    expect(getAllowedNextStatuses(CaseStatus.Assigned)).toEqual([CaseStatus.InProgress, CaseStatus.UnderReview]);
    expect(getAllowedNextStatuses(CaseStatus.InProgress)).toEqual([CaseStatus.Resolved, CaseStatus.Assigned]);
    expect(getAllowedNextStatuses(CaseStatus.Resolved)).toEqual([CaseStatus.Closed, CaseStatus.InProgress]);
  });

  it("never suggests a transition away from a terminal or pre-verification status", () => {
    expect(getAllowedNextStatuses(CaseStatus.VerificationPending)).toEqual([]);
    expect(getAllowedNextStatuses(CaseStatus.Closed)).toEqual([]);
    expect(getAllowedNextStatuses(CaseStatus.Rejected)).toEqual([]);
    expect(getAllowedNextStatuses(CaseStatus.Duplicate)).toEqual([]);
  });
});
