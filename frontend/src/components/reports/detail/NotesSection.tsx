"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Loader2, StickyNote } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { addNoteSchema, type AddNoteFormValues } from "@/lib/validation/reportSchemas";
import { formatDateTime } from "@/lib/utils/format";
import type { InternalNoteItem } from "@/types/reports";

interface NotesSectionProps {
  notes: InternalNoteItem[];
  canAddNotes: boolean;
  isSubmitting: boolean;
  onAddNote: (content: string) => void;
}

export function NotesSection({ notes, canAddNotes, isSubmitting, onAddNote }: NotesSectionProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AddNoteFormValues>({
    resolver: zodResolver(addNoteSchema),
    defaultValues: { content: "" },
  });

  const onSubmit = (values: AddNoteFormValues) => {
    onAddNote(values.content);
    reset();
  };

  return (
    <div className="flex flex-col gap-4">
      {canAddNotes && (
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-2">
          <Textarea
            placeholder="Add an internal note — visible to admins only, not the reporter."
            aria-invalid={!!errors.content}
            {...register("content")}
          />
          {errors.content && <p className="text-xs text-destructive">{errors.content.message}</p>}
          <Button type="submit" size="sm" disabled={isSubmitting} className="self-end">
            {isSubmitting && <Loader2 className="size-3.5 animate-spin" />}
            Add note
          </Button>
        </form>
      )}

      {notes.length === 0 ? (
        <p className="py-4 text-center text-sm text-muted-foreground">No internal notes yet.</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {notes.map((note) => (
            <li key={note.id} className="rounded-lg border border-border bg-muted/30 p-3">
              <div className="flex items-start gap-2">
                <StickyNote className="mt-0.5 size-3.5 shrink-0 text-muted-foreground" aria-hidden="true" />
                <div className="min-w-0 flex-1">
                  <p className="text-sm whitespace-pre-wrap text-foreground">{note.content}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {note.createdByAdminName} · {formatDateTime(note.createdAt)}
                  </p>
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
