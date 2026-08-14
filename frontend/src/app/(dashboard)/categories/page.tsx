"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Pencil, Plus } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/layout/PageHeader";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/ui/status-badge";
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
import { CategoryFormDialog } from "@/components/categories/CategoryFormDialog";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isManagerOrAbove } from "@/lib/auth/permissions";
import { getCategories, createCategory, updateCategory, disableCategory } from "@/lib/api/categories";
import { ApiError } from "@/lib/api/client";
import { priorityTone } from "@/lib/utils/statusStyles";
import type { CategoryFormValues } from "@/lib/validation/categorySchemas";
import type { Category } from "@/types/categories";

export default function CategoriesPage() {
  const { admin } = useAuth();
  const queryClient = useQueryClient();
  const canManage = admin ? isManagerOrAbove(admin.role) : false;

  const [formOpen, setFormOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [disablingCategory, setDisablingCategory] = useState<Category | null>(null);

  const { data, isLoading, isError, error } = useQuery({ queryKey: ["categories"], queryFn: getCategories });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["categories"] });

  const createMutation = useMutation({
    mutationFn: (values: CategoryFormValues) => createCategory(values),
    onSuccess: () => {
      toast.success("Category added.");
      setFormOpen(false);
      invalidate();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't add the category."),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: CategoryFormValues }) => updateCategory(id, values),
    onSuccess: () => {
      toast.success("Category updated.");
      setFormOpen(false);
      setEditingCategory(null);
      invalidate();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't update the category."),
  });

  const disableMutation = useMutation({
    mutationFn: (id: string) => disableCategory(id),
    onSuccess: () => {
      toast.success("Category disabled.");
      setDisablingCategory(null);
      invalidate();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.message : "Couldn't disable the category."),
  });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Categories"
        description="Incident categories, their default priority, and SLA clock."
        actions={
          canManage && (
            <Button
              size="sm"
              onClick={() => {
                setEditingCategory(null);
                setFormOpen(true);
              }}
            >
              <Plus />
              Add category
            </Button>
          )
        }
      />

      {isError && (
        <Alert variant="destructive">
          <AlertTriangle className="size-4" />
          <AlertTitle>Couldn&apos;t load categories</AlertTitle>
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
                <TableHead>Order</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Default priority</TableHead>
                <TableHead>SLA</TableHead>
                <TableHead>Status</TableHead>
                {canManage && <TableHead className="text-right">Actions</TableHead>}
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.map((category) => (
                <TableRow key={category.id}>
                  <TableCell className="text-muted-foreground">{category.displayOrder}</TableCell>
                  <TableCell className="font-medium">{category.name}</TableCell>
                  <TableCell className="max-w-72 truncate text-muted-foreground">{category.description}</TableCell>
                  <TableCell><StatusBadge value={category.defaultPriority} tone={priorityTone(category.defaultPriority)} /></TableCell>
                  <TableCell className="text-muted-foreground">{category.slaHours}h</TableCell>
                  <TableCell>
                    <Badge variant={category.isActive ? "outline" : "secondary"}>
                      {category.isActive ? "Active" : "Disabled"}
                    </Badge>
                  </TableCell>
                  {canManage && (
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button
                          size="icon-sm"
                          variant="ghost"
                          aria-label={`Edit ${category.name}`}
                          onClick={() => {
                            setEditingCategory(category);
                            setFormOpen(true);
                          }}
                        >
                          <Pencil />
                        </Button>
                        {category.isActive && (
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => setDisablingCategory(category)}
                          >
                            Disable
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <CategoryFormDialog
        open={formOpen}
        onOpenChange={(open) => {
          setFormOpen(open);
          if (!open) setEditingCategory(null);
        }}
        category={editingCategory}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
        onSave={(values) =>
          editingCategory
            ? updateMutation.mutate({ id: editingCategory.id, values })
            : createMutation.mutate(values)
        }
      />

      <AlertDialog open={!!disablingCategory} onOpenChange={(open) => !open && setDisablingCategory(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Disable {disablingCategory?.name}?</AlertDialogTitle>
            <AlertDialogDescription>
              Existing reports keep this category. New reports won&apos;t be able to use it until it&apos;s
              re-enabled.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              variant="destructive"
              disabled={disableMutation.isPending}
              onClick={() => disablingCategory && disableMutation.mutate(disablingCategory.id)}
            >
              Disable
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
