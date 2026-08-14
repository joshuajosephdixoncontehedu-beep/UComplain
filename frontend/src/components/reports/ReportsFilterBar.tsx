"use client";

import { useEffect, useState } from "react";
import { SearchIcon, XIcon } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { CaseStatus, IncidentPriority } from "@/types/enums";
import { humanizeEnumValue } from "@/lib/utils/statusStyles";
import type { Category } from "@/types/categories";
import type { Administrator } from "@/types/administrators";
import type { GetReportsQuery } from "@/types/reports";

export type ReportFilters = Pick<
  GetReportsQuery,
  "search" | "categoryId" | "priority" | "caseStatus" | "assignedAdminId" | "location" | "from" | "to"
>;

const ALL = "all";

interface ReportsFilterBarProps {
  filters: ReportFilters;
  onChange: (filters: ReportFilters) => void;
  categories: Category[];
  administrators: Administrator[];
}

export function ReportsFilterBar({ filters, onChange, categories, administrators }: ReportsFilterBarProps) {
  const [search, setSearch] = useState(filters.search ?? "");
  const [location, setLocation] = useState(filters.location ?? "");

  // Keep local text inputs in sync when filters are reset from outside (e.g. "Clear
  // all"), adjusted during render per React's guidance rather than in an effect —
  // this avoids the extra commit-then-recommit cascade a useEffect sync would cause.
  const [lastSearchFilter, setLastSearchFilter] = useState(filters.search);
  if (filters.search !== lastSearchFilter) {
    setLastSearchFilter(filters.search);
    setSearch(filters.search ?? "");
  }
  const [lastLocationFilter, setLastLocationFilter] = useState(filters.location);
  if (filters.location !== lastLocationFilter) {
    setLastLocationFilter(filters.location);
    setLocation(filters.location ?? "");
  }

  useEffect(() => {
    const handle = setTimeout(() => {
      if (search !== (filters.search ?? "")) onChange({ ...filters, search: search || undefined });
    }, 400);
    return () => clearTimeout(handle);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search]);

  useEffect(() => {
    const handle = setTimeout(() => {
      if (location !== (filters.location ?? "")) onChange({ ...filters, location: location || undefined });
    }, 400);
    return () => clearTimeout(handle);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location]);

  const hasActiveFilters =
    !!filters.search ||
    !!filters.categoryId ||
    !!filters.priority ||
    !!filters.caseStatus ||
    !!filters.assignedAdminId ||
    !!filters.location ||
    !!filters.from ||
    !!filters.to;

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-border bg-card p-4">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="report-search">Search</Label>
          <div className="relative">
            <SearchIcon className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="report-search"
              placeholder="Case reference or description"
              className="pl-8"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>Category</Label>
          <Select
            value={filters.categoryId ?? ALL}
            onValueChange={(value) => onChange({ ...filters, categoryId: !value || value === ALL ? undefined : value })}
          >
            <SelectTrigger className="w-full"><SelectValue placeholder="All categories" /></SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All categories</SelectItem>
              {categories.map((c) => (
                <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>Priority</Label>
          <Select
            value={filters.priority ?? ALL}
            onValueChange={(value) =>
              onChange({ ...filters, priority: value === ALL ? undefined : (value as IncidentPriority) })
            }
          >
            <SelectTrigger className="w-full"><SelectValue placeholder="All priorities" /></SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All priorities</SelectItem>
              {Object.values(IncidentPriority).map((p) => (
                <SelectItem key={p} value={p}>{p}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>Status</Label>
          <Select
            value={filters.caseStatus ?? ALL}
            onValueChange={(value) =>
              onChange({ ...filters, caseStatus: value === ALL ? undefined : (value as CaseStatus) })
            }
          >
            <SelectTrigger className="w-full"><SelectValue placeholder="All statuses" /></SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All statuses</SelectItem>
              {Object.values(CaseStatus)
                .filter((s) => s !== CaseStatus.VerificationPending)
                .map((s) => (
                  <SelectItem key={s} value={s}>{humanizeEnumValue(s)}</SelectItem>
                ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>Assigned to</Label>
          <Select
            value={filters.assignedAdminId ?? ALL}
            onValueChange={(value) => onChange({ ...filters, assignedAdminId: !value || value === ALL ? undefined : value })}
          >
            <SelectTrigger className="w-full"><SelectValue placeholder="Anyone" /></SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Anyone</SelectItem>
              {administrators.map((a) => (
                <SelectItem key={a.id} value={a.id}>{a.fullName}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="report-location">Location</Label>
          <Input
            id="report-location"
            placeholder="e.g. Bo, Kenema…"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="report-from">Reported from</Label>
          <Input
            id="report-from"
            type="date"
            value={filters.from?.slice(0, 10) ?? ""}
            onChange={(e) =>
              onChange({ ...filters, from: e.target.value ? new Date(e.target.value).toISOString() : undefined })
            }
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="report-to">Reported to</Label>
          <Input
            id="report-to"
            type="date"
            value={filters.to?.slice(0, 10) ?? ""}
            onChange={(e) =>
              onChange({ ...filters, to: e.target.value ? new Date(e.target.value).toISOString() : undefined })
            }
          />
        </div>
      </div>

      {hasActiveFilters && (
        <div>
          <Button variant="ghost" size="sm" onClick={() => onChange({})}>
            <XIcon />
            Clear all filters
          </Button>
        </div>
      )}
    </div>
  );
}
