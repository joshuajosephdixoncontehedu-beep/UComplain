import { formatDateTime } from "@/lib/utils/format";
import type { IncidentReportDetail } from "@/types/reports";

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-sm text-foreground">{value}</p>
    </div>
  );
}

export function ReportOverviewCard({ report }: { report: IncidentReportDetail }) {
  return (
    <div className="flex flex-col gap-4">
      <div>
        <p className="text-xs text-muted-foreground">Description</p>
        <p className="mt-1 text-sm whitespace-pre-wrap text-foreground">{report.description}</p>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Field label="Category" value={report.categoryName} />
        <Field label="Source channel" value={report.sourceChannel} />
        <Field label="Location" value={report.locationDescription} />
        <Field
          label="Coordinates"
          value={
            report.latitude !== null && report.longitude !== null
              ? `${report.latitude.toFixed(5)}, ${report.longitude.toFixed(5)}`
              : "Not provided"
          }
        />
        <Field label="Incident occurred" value={formatDateTime(report.incidentOccurredAt)} />
        <Field label="Reported (created)" value={formatDateTime(report.createdAt)} />
        <Field label="Last updated" value={formatDateTime(report.updatedAt)} />
        <Field label="Closed" value={formatDateTime(report.closedAt)} />
      </div>

      {report.mediaReference && (
        <div>
          <p className="text-xs text-muted-foreground">Media reference</p>
          <p className="text-sm text-foreground">{report.mediaReference}</p>
        </div>
      )}

      {report.resolutionSummary && (
        <div>
          <p className="text-xs text-muted-foreground">Resolution summary</p>
          <p className="text-sm whitespace-pre-wrap text-foreground">{report.resolutionSummary}</p>
        </div>
      )}
    </div>
  );
}
