namespace CommunityIncidentReporting.Application.Features.ReporterAccount;

/// <summary>The sweep behind data-export requests — see DataExportJob (the BackgroundService that calls this periodically).</summary>
public interface IDataExportProcessorService
{
    /// <summary>Builds and uploads the export for every Pending request. Returns how many were processed (Completed or Failed).</summary>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}
