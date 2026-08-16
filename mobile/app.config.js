/** @type {import('expo/config').ExpoConfig} */
module.exports = {
  name: 'U Complain',
  slug: 'mobile',
  version: '1.0.0',
  orientation: 'portrait',
  icon: './assets/images/icon.png',
  scheme: 'mobile',
  userInterfaceStyle: 'automatic',
  android: {
    package: 'online.ucomplain.app',
    versionCode: 1,
    adaptiveIcon: {
      foregroundImage: './assets/images/android-icon-foreground.png',
    },
    predictiveBackGestureEnabled: false,
    permissions: [
      'android.permission.RECORD_AUDIO',
      'android.permission.MODIFY_AUDIO_SETTINGS',
      'android.permission.ACCESS_COARSE_LOCATION',
      'android.permission.ACCESS_FINE_LOCATION',
    ],
    config: {
      googleMaps: {
        // Required for the Map tab to render real tiles on Android — a Google Cloud
        // project with the "Maps SDK for Android" enabled, restricted to this app's
        // package name (online.ucomplain.app) + release/dev-client SHA-1 fingerprint.
        // Without it, the map view still renders but shows no tiles.
        apiKey: process.env.GOOGLE_MAPS_API_KEY ?? '',
      },
    },
  },
  web: {
    output: 'static',
    favicon: './assets/images/favicon.png',
  },
  plugins: [
    'expo-router',
    [
      'expo-splash-screen',
      {
        backgroundColor: '#1D4ED8',
        image: './assets/images/splash-icon.png',
        imageWidth: 120,
      },
    ],
    '@react-native-community/datetimepicker',
    [
      'expo-audio',
      {
        microphonePermission: 'U Complain uses your microphone to record an optional voice note for your incident report.',
      },
    ],
    [
      'expo-image-picker',
      {
        photosPermission: 'U Complain uses your photo library to attach evidence to your incident report.',
        cameraPermission: 'U Complain uses your camera to attach evidence to your incident report.',
      },
    ],
    [
      'expo-location',
      {
        locationWhenInUsePermission: 'U Complain uses your location to pinpoint the incident and show reports near you.',
      },
    ],
    'expo-asset',
    [
      'expo-maps',
      {
        requestLocationPermission: true,
        locationPermission: 'U Complain uses your location to center the map and show incidents near you.',
      },
    ],
  ],
  experiments: {
    typedRoutes: true,
    reactCompiler: true,
  },
  extra: {
    router: {},
    eas: {
      projectId: 'd24df935-85c7-4662-ad9f-0b114c3093d0',
    },
  },
  owner: 'jjdixon',
};
