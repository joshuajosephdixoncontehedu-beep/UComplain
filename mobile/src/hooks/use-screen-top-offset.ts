import { useSafeAreaInsets } from 'react-native-safe-area-context';

/**
 * Every screen previously hardcoded its top offset as a literal pixel value (e.g.
 * `mt-[68px]`) tuned against one specific device's status bar height. That clips
 * content under the status bar/notch on any device with a taller inset (Dynamic
 * Island, punch-hole cameras, etc.) and leaves too much dead space on devices with
 * a shorter one. This computes the real offset from the device's actual safe-area
 * inset instead, so headers land at a consistent visual distance below the notch
 * on every screen.
 */
export function useScreenTopOffset(extra = 20) {
  const insets = useSafeAreaInsets();
  return insets.top + extra;
}
