import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { badgeToneClasses, type BadgeTone, humanizeEnumValue } from "@/lib/utils/statusStyles";

interface StatusBadgeProps {
  value: string;
  tone: BadgeTone;
  className?: string;
}

export function StatusBadge({ value, tone, className }: StatusBadgeProps) {
  return (
    <Badge variant="outline" className={cn(badgeToneClasses[tone], "font-medium", className)}>
      {humanizeEnumValue(value)}
    </Badge>
  );
}
