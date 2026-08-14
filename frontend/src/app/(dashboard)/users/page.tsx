"use client";

import { useState } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { AlertTriangle } from "lucide-react";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { StatusBadge } from "@/components/ui/status-badge";
import { ReportsPagination } from "@/components/reports/ReportsPagination";
import { getReporters } from "@/lib/api/reporters";
import { ApiError } from "@/lib/api/client";
import { verificationStatusTone } from "@/lib/utils/statusStyles";
import { formatDate } from "@/lib/utils/format";
import { VerificationStatus } from "@/types/enums";

const PAGE_SIZE = 20;
const ALL = "all";

export default function UsersPage() {
  const [search, setSearch] = useState("");
  const [verificationStatus, setVerificationStatus] = useState<VerificationStatus | undefined>(undefined);
  const [isRestricted, setIsRestricted] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);

  const query = { search: search || undefined, verificationStatus, isRestricted, page, pageSize: PAGE_SIZE };
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["reporters", query],
    queryFn: () => getReporters(query),
  });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Users" description="Reporters who have submitted incidents." />

      <div className="grid grid-cols-1 gap-3 rounded-lg border border-border bg-card p-4 sm:grid-cols-3">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="user-search">Search</Label>
          <Input
            id="user-search"
            placeholder="Masked contact reference"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>Verification status</Label>
          <Select
            value={verificationStatus ?? ALL}
            onValueChange={(value) => {
              setVerificationStatus(!value || value === ALL ? undefined : (value as VerificationStatus));
              setPage(1);
            }}
          >
            <SelectTrigger className="w-full"><SelectValue placeholder="Any" /></SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Any</SelectItem>
              {Object.values(VerificationStatus).map((s) => (
                <SelectItem key={s} value={s}>{s}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label>Restriction</Label>
          <Select
            value={isRestricted === undefined ? ALL : String(isRestricted)}
            onValueChange={(value) => {
              setIsRestricted(!value || value === ALL ? undefined : value === "true");
              setPage(1);
            }}
          >
            <SelectTrigger className="w-full"><SelectValue placeholder="Any" /></SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Any</SelectItem>
              <SelectItem value="true">Restricted</SelectItem>
              <SelectItem value="false">Not restricted</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load users</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && <Skeleton className="h-64 rounded-lg" />}

      {data && (
        <>
          <div className="rounded-lg border border-border bg-card">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Contact (masked)</TableHead>
                  <TableHead>Verification</TableHead>
                  <TableHead>Restricted</TableHead>
                  <TableHead>Consent given</TableHead>
                  <TableHead>Reports</TableHead>
                  <TableHead>First seen</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.items.map((reporter) => (
                  <TableRow key={reporter.id}>
                    <TableCell>
                      <Link href={`/users/${reporter.id}`} className="font-medium text-primary hover:underline">
                        {reporter.maskedContactReference}
                      </Link>
                    </TableCell>
                    <TableCell>
                      <StatusBadge value={reporter.verificationStatus} tone={verificationStatusTone(reporter.verificationStatus)} />
                    </TableCell>
                    <TableCell>
                      {reporter.isRestricted ? <Badge variant="destructive">Restricted</Badge> : <span className="text-muted-foreground">—</span>}
                    </TableCell>
                    <TableCell className="text-muted-foreground">{formatDate(reporter.consentAt)}</TableCell>
                    <TableCell className="text-muted-foreground">{reporter.reportCount}</TableCell>
                    <TableCell className="text-muted-foreground">{formatDate(reporter.createdAt)}</TableCell>
                  </TableRow>
                ))}
                {data.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} className="py-8 text-center text-sm text-muted-foreground">
                      No users match these filters.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
          <ReportsPagination page={page} pageSize={PAGE_SIZE} total={data.total} onPageChange={setPage} />
        </>
      )}
    </div>
  );
}
