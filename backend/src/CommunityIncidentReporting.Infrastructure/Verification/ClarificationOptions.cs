namespace CommunityIncidentReporting.Infrastructure.Verification;

public class ClarificationOptions
{
    public const string SectionName = "Clarification";

    /// <summary>How long a reporter has to respond before ClarificationAutoCloseService closes the report.</summary>
    public int DeadlineHours { get; set; } = 48;

    /// <summary>How often ClarificationAutoCloseJob sweeps for overdue requests.</summary>
    public int AutoCloseSweepIntervalMinutes { get; set; } = 30;
}
