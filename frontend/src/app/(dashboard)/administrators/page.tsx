"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Pencil, Plus, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { CreateAdministratorDialog } from "@/components/administrators/CreateAdministratorDialog";
import { EditAdministratorDialog } from "@/components/administrators/EditAdministratorDialog";
import {
  getAdministrators,
  createAdministrator,
  updateAdministrator,
  deactivateAdministrator,
  reactivateAdministrator,
} from "@/lib/api/administrators";
import { ApiError } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isSuperAdmin, roleLabel } from "@/lib/auth/permissions";
import { formatDateTime } from "@/lib/utils/format";
import { AdminRole } from "@/types/enums";
import type {
  CreateAdministratorFormValues,
  UpdateAdministratorFormValues,
} from "@/lib/validation/administratorSchemas";
import type { Administrator } from "@/types/administrators";

export default function AdministratorsPage() {
  const { admin: currentAdmin } = useAuth();
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [editingAdmin, setEditingAdmin] = useState<Administrator | null>(null);
  const [deactivatingAdmin, setDeactivatingAdmin] = useState<Administrator | null>(null);

  const canManage = currentAdmin ? isSuperAdmin(currentAdmin.role) : false;

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ["administrators"],
    queryFn: getAdministrators,
    enabled: canManage,
  });

  const activeSuperAdminCount = (data ?? []).filter((a) => a.role === AdminRole.SuperAdmin && a.isActive).length;

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administrators"] });

  const createMutation = useMutation({
    mutationFn: (values: CreateAdministratorFormValues) => createAdministrator(values),
    onSuccess: () => {
      toast.success("Administrator added.");
      setCreateOpen(false);
      invalidate();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't add the administrator."),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: UpdateAdministratorFormValues }) =>
      updateAdministrator(id, values),
    onSuccess: () => {
      toast.success("Administrator updated.");
      setEditingAdmin(null);
      invalidate();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't update the administrator."),
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateAdministrator(id),
    onSuccess: () => {
      toast.success("Administrator deactivated.");
      setDeactivatingAdmin(null);
      invalidate();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't deactivate the administrator."),
  });

  const reactivateMutation = useMutation({
    mutationFn: (id: string) => reactivateAdministrator(id),
    onSuccess: () => {
      toast.success("Administrator reactivated.");
      invalidate();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't reactivate the administrator."),
  });

  if (currentAdmin && !canManage) {
    return (
      <div className="flex flex-col gap-6">
        <PageHeader title="Administrators" description="Manage portal administrator accounts and roles." />
        <Alert>
          <ShieldOff className="size-4" />
          <AlertTitle>Access restricted</AlertTitle>
          <AlertDescription>Only Super Admins can manage administrator accounts.</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Administrators"
        description="Manage portal administrator accounts and roles."
        actions={
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <Plus />
            Add administrator
          </Button>
        }
      />

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load administrators</AlertTitle>
          <AlertDescription>
            {error instanceof ApiError ? error.message : "An unexpected error occurred. Please try again."}
          </AlertDescription>
        </Alert>
      )}

      {isLoading && <Skeleton className="h-64 rounded-lg" />}

      {data && (
        <div className="rounded-lg border border-border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Role</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Last login</TableHead>
                <TableHead>Created</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((admin) => {
                const isLastActiveSuperAdmin =
                  admin.role === AdminRole.SuperAdmin && admin.isActive && activeSuperAdminCount <= 1;
                return (
                  <TableRow key={admin.id}>
                    <TableCell className="font-medium">{admin.fullName}</TableCell>
                    <TableCell className="text-muted-foreground">{admin.email}</TableCell>
                    <TableCell>{roleLabel(admin.role)}</TableCell>
                    <TableCell>
                      <Badge variant={admin.isActive ? "outline" : "secondary"}>
                        {admin.isActive ? "Active" : "Deactivated"}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{formatDateTime(admin.lastLoginAt)}</TableCell>
                    <TableCell className="text-muted-foreground">{formatDateTime(admin.createdAt)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          aria-label={`Edit ${admin.fullName}`}
                          onClick={() => setEditingAdmin(admin)}
                        >
                          <Pencil />
                        </Button>
                        {admin.isActive ? (
                          <Button
                            size="sm"
                            variant="ghost"
                            disabled={isLastActiveSuperAdmin}
                            title={isLastActiveSuperAdmin ? "The last active Super Admin can't be deactivated" : undefined}
                            onClick={() => setDeactivatingAdmin(admin)}
                          >
                            Deactivate
                          </Button>
                        ) : (
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => reactivateMutation.mutate(admin.id)}
                          >
                            Reactivate
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </div>
      )}

      <CreateAdministratorDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        isSubmitting={createMutation.isPending}
        onSave={(values) => createMutation.mutate(values)}
      />

      <EditAdministratorDialog
        open={!!editingAdmin}
        onOpenChange={(open) => !open && setEditingAdmin(null)}
        administrator={editingAdmin}
        isSubmitting={updateMutation.isPending}
        onSave={(values) => editingAdmin && updateMutation.mutate({ id: editingAdmin.id, values })}
      />

      <AlertDialog open={!!deactivatingAdmin} onOpenChange={(open) => !open && setDeactivatingAdmin(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Deactivate {deactivatingAdmin?.fullName}?</AlertDialogTitle>
            <AlertDialogDescription>
              They&apos;ll immediately lose access to the portal. This can be reversed later by reactivating the
              account.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={deactivateMutation.isPending}
              onClick={() => deactivatingAdmin && deactivateMutation.mutate(deactivatingAdmin.id)}
            >
              Deactivate
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
