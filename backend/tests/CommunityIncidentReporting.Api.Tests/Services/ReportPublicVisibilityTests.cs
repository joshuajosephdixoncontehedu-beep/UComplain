using CommunityIncidentReporting.Application.Features.PublicMap;
using CommunityIncidentReporting.Domain.Enums;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Services;

public class ReportPublicVisibilityTests
{
    [Theory]
    [InlineData(VerificationStatus.Verified, CaseStatus.UnderReview, true, true)]
    [InlineData(VerificationStatus.Verified, CaseStatus.Assigned, true, true)]
    [InlineData(VerificationStatus.Verified, CaseStatus.InProgress, true, true)]
    [InlineData(VerificationStatus.Verified, CaseStatus.Resolved, true, true)]
    [InlineData(VerificationStatus.Verified, CaseStatus.Closed, true, true)]
    // Never visible, regardless of ShowOnPublicMap, once withdrawn.
    [InlineData(VerificationStatus.Verified, CaseStatus.Withdrawn, true, false)]
    // Never visible before verification, regardless of ShowOnPublicMap.
    [InlineData(VerificationStatus.Pending, CaseStatus.VerificationPending, true, false)]
    [InlineData(VerificationStatus.NeedsClarification, CaseStatus.VerificationPending, true, false)]
    [InlineData(VerificationStatus.Rejected, CaseStatus.Rejected, true, false)]
    [InlineData(VerificationStatus.SuspectedDuplicate, CaseStatus.Duplicate, true, false)]
    // The reporter's own opt-out always wins, even on an otherwise-eligible report.
    [InlineData(VerificationStatus.Verified, CaseStatus.UnderReview, false, false)]
    [InlineData(VerificationStatus.Verified, CaseStatus.Resolved, false, false)]
    public void Compute_MatchesTheDocumentedRule(
        VerificationStatus verificationStatus, CaseStatus caseStatus, bool showOnPublicMap, bool expected)
    {
        var result = ReportPublicVisibility.Compute(verificationStatus, caseStatus, showOnPublicMap);

        result.Should().Be(expected);
    }
}
