using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Domain.Enums;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Services;

public class ReportStatusProjectionTests
{
    [Theory]
    [InlineData(VerificationStatus.Pending, CaseStatus.VerificationPending, "Awaiting verification", 1, 15, ReportListBucket.Active)]
    [InlineData(VerificationStatus.NeedsClarification, CaseStatus.VerificationPending, "Needs clarification", 1, 25, ReportListBucket.Active)]
    [InlineData(VerificationStatus.FlaggedAbuse, CaseStatus.VerificationPending, "Under review", 1, 25, ReportListBucket.Active)]
    [InlineData(VerificationStatus.Rejected, CaseStatus.Rejected, "Rejected", null, 0, ReportListBucket.Rejected)]
    [InlineData(VerificationStatus.SuspectedDuplicate, CaseStatus.Duplicate, "Duplicate", null, 0, ReportListBucket.Rejected)]
    [InlineData(VerificationStatus.Verified, CaseStatus.UnderReview, "Verified", 3, 60, ReportListBucket.Active)]
    [InlineData(VerificationStatus.Verified, CaseStatus.Assigned, "Assigned", 3, 70, ReportListBucket.Active)]
    [InlineData(VerificationStatus.Verified, CaseStatus.InProgress, "In progress", 4, 85, ReportListBucket.Active)]
    [InlineData(VerificationStatus.Verified, CaseStatus.Resolved, "Resolved", 5, 100, ReportListBucket.Resolved)]
    [InlineData(VerificationStatus.Verified, CaseStatus.Closed, "Closed", 5, 100, ReportListBucket.Resolved)]
    public void Project_ForEveryReachableCombination_MatchesTheDesignedTable(
        VerificationStatus verificationStatus, CaseStatus caseStatus,
        string expectedBadge, int? expectedStage, int expectedProgress, ReportListBucket expectedBucket)
    {
        var result = ReportStatusProjection.Project(verificationStatus, caseStatus);

        result.Should().Be(new ReportStatusProjectionDto(expectedBadge, expectedStage, expectedProgress, expectedBucket));
    }

    [Theory]
    [InlineData(VerificationStatus.Pending)]
    [InlineData(VerificationStatus.Verified)]
    [InlineData(VerificationStatus.NeedsClarification)]
    [InlineData(VerificationStatus.Rejected)]
    public void Project_WithCaseStatusWithdrawn_AlwaysWinsRegardlessOfVerificationStatus(VerificationStatus verificationStatus)
    {
        var result = ReportStatusProjection.Project(verificationStatus, CaseStatus.Withdrawn);

        result.Should().Be(new ReportStatusProjectionDto("Withdrawn", null, 0, ReportListBucket.NotListed));
    }

    [Fact]
    public void Project_NeverThrowsForAnyEnumCombination()
    {
        foreach (var verificationStatus in Enum.GetValues<VerificationStatus>())
        {
            foreach (var caseStatus in Enum.GetValues<CaseStatus>())
            {
                var act = () => ReportStatusProjection.Project(verificationStatus, caseStatus);
                act.Should().NotThrow($"({verificationStatus}, {caseStatus}) must always project to something");
            }
        }
    }
}
