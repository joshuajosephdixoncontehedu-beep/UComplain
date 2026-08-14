import { describe, expect, it } from "vitest";
import { caseStatusTone, humanizeEnumValue, humanizePascalCase, priorityTone, verificationStatusTone } from "./statusStyles";
import { CaseStatus, IncidentPriority, VerificationStatus } from "@/types/enums";

describe("caseStatusTone", () => {
  it("uses semantic tones consistent with the status's meaning", () => {
    expect(caseStatusTone(CaseStatus.Resolved)).toBe("green");
    expect(caseStatusTone(CaseStatus.Rejected)).toBe("red");
    expect(caseStatusTone(CaseStatus.InProgress)).toBe("amber");
  });
});

describe("verificationStatusTone", () => {
  it("marks Verified as green and rejection-adjacent states as red", () => {
    expect(verificationStatusTone(VerificationStatus.Verified)).toBe("green");
    expect(verificationStatusTone(VerificationStatus.Rejected)).toBe("red");
    expect(verificationStatusTone(VerificationStatus.FlaggedAbuse)).toBe("red");
  });
});

describe("priorityTone", () => {
  it("escalates tone with priority", () => {
    expect(priorityTone(IncidentPriority.Low)).toBe("slate");
    expect(priorityTone(IncidentPriority.Critical)).toBe("red");
  });
});

describe("humanizeEnumValue", () => {
  it("adds spacing to known PascalCase enum values", () => {
    expect(humanizeEnumValue("UnderReview")).toBe("Under Review");
    expect(humanizeEnumValue("NeedsClarification")).toBe("Needs Clarification");
  });

  it("falls back to the raw value for anything not in the lookup", () => {
    expect(humanizeEnumValue("Resolved")).toBe("Resolved");
  });
});

describe("humanizePascalCase", () => {
  it("splits generic PascalCase action names into words", () => {
    expect(humanizePascalCase("ReportStatusChanged")).toBe("Report Status Changed");
    expect(humanizePascalCase("VerificationDecisionRecorded")).toBe("Verification Decision Recorded");
  });
});
