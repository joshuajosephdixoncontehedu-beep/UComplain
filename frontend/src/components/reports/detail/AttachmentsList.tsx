"use client";

import { useState } from "react";
import { Download, FileText, Image as ImageIcon, Loader2, Music, Video } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { ApiError } from "@/lib/api/client";
import { getReportAttachmentUrl } from "@/lib/api/reports";
import { formatDateTime } from "@/lib/utils/format";
import { MediaType } from "@/types/enums";
import type { MediaAttachmentItem } from "@/types/reports";

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

const ICON_BY_TYPE: Record<MediaType, typeof ImageIcon> = {
  [MediaType.Image]: ImageIcon,
  [MediaType.Video]: Video,
  [MediaType.Audio]: Music,
  [MediaType.Document]: FileText,
};

function AttachmentRow({ reportId, attachment }: { reportId: string; attachment: MediaAttachmentItem }) {
  const [loading, setLoading] = useState(false);
  const [audioUrl, setAudioUrl] = useState<string | null>(null);
  const Icon = ICON_BY_TYPE[attachment.mediaType];
  const isAudio = attachment.mediaType === MediaType.Audio;

  const onView = async () => {
    setLoading(true);
    try {
      // Signed URL is short-lived — always fetched fresh on click, never cached.
      const { url } = await getReportAttachmentUrl(reportId, attachment.id);
      if (isAudio) {
        setAudioUrl(url);
      } else {
        window.open(url, "_blank", "noopener,noreferrer");
      }
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : "Couldn't load this attachment.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-2 rounded-md border border-border p-3">
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-2.5">
          <Icon className="size-4 shrink-0 text-muted-foreground" />
          <div className="min-w-0">
            <p className="truncate text-sm text-foreground">{attachment.fileName}</p>
            <p className="text-xs text-muted-foreground">
              {formatFileSize(attachment.fileSizeBytes)} · {formatDateTime(attachment.uploadedAt)}
            </p>
          </div>
        </div>
        <Button size="sm" variant="outline" onClick={onView} disabled={loading}>
          {loading ? <Loader2 className="size-3.5 animate-spin" /> : isAudio ? <Music className="size-3.5" /> : <Download className="size-3.5" />}
          {isAudio ? "Play" : "View"}
        </Button>
      </div>
      {audioUrl && <audio controls src={audioUrl} className="w-full" />}
    </div>
  );
}

export function AttachmentsList({ reportId, items }: { reportId: string; items: MediaAttachmentItem[] }) {
  if (items.length === 0) {
    return <p className="text-sm text-muted-foreground">No photos, audio, or other files were attached to this report.</p>;
  }

  return (
    <div className="flex flex-col gap-2">
      {items
        .slice()
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((attachment) => (
          <AttachmentRow key={attachment.id} reportId={reportId} attachment={attachment} />
        ))}
    </div>
  );
}
