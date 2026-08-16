import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import {
  RecordingPresets,
  requestRecordingPermissionsAsync,
  setAudioModeAsync,
  useAudioPlayer,
  useAudioPlayerStatus,
  useAudioRecorder,
  useAudioRecorderState,
} from 'expo-audio';
import { router } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Text, View } from 'react-native';

import { AppButton } from '@/components/ui/button';
import { InfoBanner } from '@/components/ui/info-banner';
import { useReportDraft } from '@/components/report/report-draft-context';
import { Waveform } from '@/components/report/waveform';
import { WizardHeader } from '@/components/report/wizard-header';

const MAX_PHOTOS = 5;

function formatDuration(seconds: number): string {
  const total = Math.max(0, Math.floor(seconds));
  const mm = Math.floor(total / 60);
  const ss = total % 60;
  return `${mm}:${ss.toString().padStart(2, '0')}`;
}

/**
 * Figma "11 New report · Evidence" (node 11:80) — wizard step 4 of 4, optional.
 * Photos and the voice note are picked/recorded with the device's real camera,
 * photo library, and microphone, then uploaded to the real draft attachment
 * endpoints (POST /reports/drafts/{id}/attachments) — no placeholders.
 */
export default function ReportEvidence() {
  const { draft, uploadPhoto, removePhoto, uploadVoiceNote, removeVoiceNote, voiceNoteLocalUri } = useReportDraft();
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [removingPhotoId, setRemovingPhotoId] = useState<string | null>(null);
  const [uploadingVoiceNote, setUploadingVoiceNote] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
  const recorderState = useAudioRecorderState(recorder, 250);

  // Real playback of the just-recorded voice note — recreates automatically when
  // voiceNoteLocalUri changes (record a new one, or clear it after removing).
  const player = useAudioPlayer(voiceNoteLocalUri ? { uri: voiceNoteLocalUri } : null);
  const playerStatus = useAudioPlayerStatus(player);

  const togglePlayback = async () => {
    if (playerStatus.playing) {
      player.pause();
      return;
    }
    // Voice notes were recorded with allowsRecording — re-enable normal playback
    // routing (speaker, respects silent switch off) before playing them back.
    await setAudioModeAsync({ allowsRecording: false, playsInSilentMode: true });
    if (playerStatus.didJustFinish) await player.seekTo(0);
    player.play();
  };

  const pickPhoto = async (source: 'camera' | 'gallery') => {
    setError(null);
    try {
      const permission =
        source === 'camera' ? await ImagePicker.requestCameraPermissionsAsync() : await ImagePicker.requestMediaLibraryPermissionsAsync();
      if (!permission.granted) {
        setError(`${source === 'camera' ? 'Camera' : 'Photo library'} permission was denied.`);
        return;
      }
      const result =
        source === 'camera'
          ? await ImagePicker.launchCameraAsync({ mediaTypes: ['images'], quality: 0.7 })
          : await ImagePicker.launchImageLibraryAsync({ mediaTypes: ['images'], quality: 0.7, selectionLimit: MAX_PHOTOS - draft.photos.length });
      if (result.canceled) return;

      setUploadingPhoto(true);
      for (const asset of result.assets) {
        await uploadPhoto({
          uri: asset.uri,
          name: asset.fileName ?? `photo-${Date.now()}.jpg`,
          mimeType: asset.mimeType ?? 'image/jpeg',
        });
      }
    } catch {
      setError('Could not upload that photo. Please try again.');
    } finally {
      setUploadingPhoto(false);
    }
  };

  const onRemovePhoto = async (id: string) => {
    setRemovingPhotoId(id);
    try {
      await removePhoto(id);
    } catch {
      setError('Could not remove that photo. Please try again.');
    } finally {
      setRemovingPhotoId(null);
    }
  };

  const startRecording = async () => {
    setError(null);
    const permission = await requestRecordingPermissionsAsync();
    if (!permission.granted) {
      setError('Microphone permission was denied.');
      return;
    }
    await setAudioModeAsync({ allowsRecording: true, playsInSilentMode: true });
    await recorder.prepareToRecordAsync();
    recorder.record();
  };

  const stopRecording = async () => {
    await recorder.stop();
    if (!recorder.uri) return;
    setUploadingVoiceNote(true);
    try {
      // 'audio/mp4' (not 'audio/m4a') — matches MediaTypeDetector's allowlist on the
      // backend; M4A is an MPEG-4/ISO-BMFF container, same magic bytes either way.
      await uploadVoiceNote({ uri: recorder.uri, name: `voice-note-${Date.now()}.m4a`, mimeType: 'audio/mp4' });
    } catch {
      setError('Could not upload the voice note. Please try again.');
    } finally {
      setUploadingVoiceNote(false);
    }
  };

  return (
    <ScrollView className="flex-1 bg-canvas" contentContainerClassName="px-5 pb-10">
      <WizardHeader step={4} />

      <View className="mt-8 gap-1.5">
        <Text className="text-h1 text-ink">Add evidence</Text>
        <Text className="text-body text-secondary">Photos make verification faster. This step is optional.</Text>
      </View>

      <View className="mt-9 flex-row flex-wrap gap-2.5">
        {draft.photos.map((photo) => (
          <View key={photo.id} className="h-[109px] w-[109px] items-center justify-end rounded-card bg-surface-muted p-2.5">
            <Ionicons name="image-outline" size={26} color="#64748B" style={{ marginBottom: 20 }} />
            <Text className="text-[11px] text-muted" numberOfLines={1}>
              {photo.fileName}
            </Text>
            {removingPhotoId === photo.id ? (
              <View className="absolute right-1.5 top-1.5 h-6 w-6 items-center justify-center rounded-full bg-ink/60">
                <ActivityIndicator size="small" color="#FFFFFF" />
              </View>
            ) : (
              <Pressable
                onPress={() => onRemovePhoto(photo.id)}
                hitSlop={6}
                className="absolute right-1.5 top-1.5 h-6 w-6 items-center justify-center rounded-full bg-ink/60">
                <Ionicons name="close" size={14} color="#FFFFFF" />
              </Pressable>
            )}
          </View>
        ))}
        {draft.photos.length < MAX_PHOTOS ? (
          <Pressable
            onPress={() => pickPhoto('gallery')}
            disabled={uploadingPhoto}
            className="h-[109px] w-[109px] items-center justify-center gap-1.5 rounded-card border border-dashed border-border">
            {uploadingPhoto ? <ActivityIndicator color="#64748B" /> : <Ionicons name="add" size={22} color="#64748B" />}
            <Text className="text-caption text-muted">Add</Text>
          </Pressable>
        ) : null}
      </View>

      <View className="mt-6 flex-row gap-3">
        <Pressable
          onPress={() => pickPhoto('camera')}
          disabled={uploadingPhoto || draft.photos.length >= MAX_PHOTOS}
          className="h-12 flex-1 flex-row items-center justify-center gap-2 rounded-input border border-border bg-surface disabled:opacity-50">
          <Ionicons name="camera-outline" size={18} color="#334155" />
          <Text className="text-body font-semibold text-ink">Take photo</Text>
        </Pressable>
        <Pressable
          onPress={() => pickPhoto('gallery')}
          disabled={uploadingPhoto || draft.photos.length >= MAX_PHOTOS}
          className="h-12 flex-1 flex-row items-center justify-center gap-2 rounded-input border border-border bg-surface disabled:opacity-50">
          <Ionicons name="images-outline" size={18} color="#334155" />
          <Text className="text-body font-semibold text-ink">From gallery</Text>
        </Pressable>
      </View>

      {draft.voiceNote ? (
        <View className="mt-4 flex-row items-center gap-3 rounded-input border border-border bg-surface p-3.5">
          <Pressable
            onPress={togglePlayback}
            disabled={!voiceNoteLocalUri}
            hitSlop={4}
            className="h-[38px] w-[38px] items-center justify-center rounded-full bg-brand-tint disabled:opacity-50">
            <Ionicons name={playerStatus.playing ? 'pause' : 'play'} size={16} color="#1D4ED8" />
          </Pressable>
          <View className="flex-1 gap-1.5">
            <View className="flex-row items-center justify-between">
              <Text className="text-body font-semibold text-ink">Voice note</Text>
              {playerStatus.duration > 0 ? (
                <Text className="text-caption text-muted">
                  {formatDuration(playerStatus.currentTime)} / {formatDuration(playerStatus.duration)}
                </Text>
              ) : null}
            </View>
            <Waveform progress={playerStatus.duration > 0 ? playerStatus.currentTime / playerStatus.duration : 0} />
          </View>
          <Pressable onPress={() => removeVoiceNote()} hitSlop={8}>
            <Ionicons name="trash-outline" size={18} color="#64748B" />
          </Pressable>
        </View>
      ) : recorderState.isRecording ? (
        <Pressable
          onPress={stopRecording}
          className="mt-4 h-12 flex-row items-center justify-center gap-2 rounded-input border border-status-critical bg-status-critical-tint">
          <Ionicons name="stop-circle-outline" size={18} color="#B91C1C" />
          <Text className="text-body font-semibold text-status-critical">
            Stop recording · {Math.floor(recorderState.durationMillis / 1000)}s
          </Text>
        </Pressable>
      ) : (
        <Pressable
          onPress={startRecording}
          disabled={uploadingVoiceNote}
          className="mt-4 h-12 flex-row items-center justify-center gap-2 rounded-input border border-border bg-surface disabled:opacity-50">
          {uploadingVoiceNote ? <ActivityIndicator color="#334155" /> : <Ionicons name="mic-outline" size={18} color="#334155" />}
          <Text className="text-body font-semibold text-ink">{uploadingVoiceNote ? 'Uploading…' : 'Record a voice note'}</Text>
        </Pressable>
      )}

      {error ? <Text className="mt-4 text-body-sm text-status-critical">{error}</Text> : null}

      <View className="mt-6">
        <InfoBanner icon="eye-off-outline" text="Photos are stored exactly as captured — review before attaching anything that identifies bystanders." />
      </View>

      <View className="mt-8 gap-3">
        <AppButton title="Review report" onPress={() => router.push('/(app)/report/review')} />
        <AppButton title="Skip for now" variant="secondary" onPress={() => router.push('/(app)/report/review')} />
      </View>
    </ScrollView>
  );
}
