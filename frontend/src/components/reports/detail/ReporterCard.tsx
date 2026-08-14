import { ShieldAlert } from "lucide-react";
import { Badge } from "@/components/ui/badge";

interface ReporterCardProps {
  maskedContact: string;
  isRestricted: boolean;
}

export function ReporterCard({ maskedContact, isRestricted }: ReporterCardProps) {
  return (
    <div className="flex flex-col gap-3">
      <div>
        <p className="text-xs text-muted-foreground">Contact (masked)</p>
        <p className="text-sm font-medium text-foreground">{maskedContact}</p>
      </div>
      {isRestricted && (
        <Badge variant="destructive" className="w-fit">
          <ShieldAlert />
          Restricted reporter
        </Badge>
      )}
      <p className="text-xs text-muted-foreground">
        Full contact details are never shown here — only the masked reference used to correlate reports from the
        same reporter.
      </p>
    </div>
  );
}
