namespace CommunityIncidentReporting.Application.Features.Dashboard.Dtos;

public record VerificationQueueSnapshotDto(
    int Pending, int NeedsClarification, int SuspectedDuplicate, int FlaggedAbuse, int Rejected);
