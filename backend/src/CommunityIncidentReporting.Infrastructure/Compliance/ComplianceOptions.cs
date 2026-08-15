namespace CommunityIncidentReporting.Infrastructure.Compliance;

public class ComplianceOptions
{
    public const string SectionName = "Compliance";

    /// <summary>How long a reporter has to cancel their own account-deletion request before AccountDeletionJob executes it.</summary>
    public int AccountDeletionGracePeriodDays { get; set; } = 14;

    public int DataExportSweepIntervalMinutes { get; set; } = 5;
    public int AccountDeletionSweepIntervalMinutes { get; set; } = 60;
    public int RetentionPurgeSweepIntervalHours { get; set; } = 24;

    /// <summary>How long a data-export download link stays valid once issued.</summary>
    public int DataExportSignedUrlExpirySeconds { get; set; } = 300;
}
