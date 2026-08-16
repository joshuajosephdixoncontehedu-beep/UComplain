# Setting up a Google Maps API key (Android)

The Map tab now renders a real `expo-maps` view. On Android it uses Google Maps, which
needs an API key tied to your app — without it, the map view still shows up but with no
tiles (a blank grey map). Apple Maps on iOS needs no key at all.

## 1. Create/select a Google Cloud project

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Top-left project dropdown → **New Project** (or pick an existing one).
3. Give it any name, e.g. "UComplain".

## 2. Enable billing

Google Maps Platform requires a billing account even within the free monthly credit
($200/month, which comfortably covers normal app usage). **Billing → Link a billing
account**, add a card. You will not be charged unless you exceed the free tier.

## 3. Enable the Maps SDK for Android

1. Left sidebar → **APIs & Services → Library**.
2. Search "Maps SDK for Android".
3. Click it → **Enable**.

## 4. Get your app's SHA-1 certificate fingerprint

Google restricts the key to a specific app package + signing certificate, so it needs the
SHA-1 fingerprint of whichever keystore signs your build. Since EAS manages the signing
keystore for you (generated automatically during the first build), get it from the Expo
dashboard rather than a local keystore file:

1. Go to `https://expo.dev/accounts/jjdixon/projects/mobile/credentials`
2. Under **Android** → select the keystore in use for this project.
3. Copy the **SHA-1 Fingerprint** shown there (looks like `AB:CD:12:34:...`).

If you later create a *production* build with a different (Play Store upload) keystore,
repeat this step for that keystore too and add a second SHA-1 in step 5.

## 5. Create the API key

1. Cloud Console → **APIs & Services → Credentials**.
2. **Create Credentials → API key.** A key is generated — do not use it unrestricted; continue to the restriction step below immediately.
3. Click the new key to edit it.
4. **Application restrictions** → **Android apps** → **Add an item**:
   - Package name: `online.ucomplain.app`
   - SHA-1 certificate fingerprint: paste the value from step 4
5. **API restrictions** → **Restrict key** → check only **Maps SDK for Android**.
6. Save.

## 6. Wire the key into the app

The app already reads this from an environment variable (`mobile/app.config.js`):

```js
android: {
  config: {
    googleMaps: {
      apiKey: process.env.GOOGLE_MAPS_API_KEY ?? '',
    },
  },
},
```

Two places need the value:

**Local development** — add to `mobile/.env.local`:
```
GOOGLE_MAPS_API_KEY=your-key-here
```

**EAS cloud builds** — add to `mobile/eas.json`'s `build.development`/`build.preview`/`build.production` profiles' `env` blocks, e.g.:
```json
"development": {
  "env": {
    "EXPO_PUBLIC_API_BASE_URL": "https://api.ucomplain.online",
    "GOOGLE_MAPS_API_KEY": "your-key-here"
  }
}
```
(Repeat for whichever profiles you build with. Since this key is restricted to your exact
package name + SHA-1, it's safe to commit in `eas.json` — it can't be used from anywhere
else, unlike a service-role/secret key.)

## 7. Rebuild

The API key gets baked into `AndroidManifest.xml` at build time — a JS-only reload isn't
enough, you need a fresh native build:

```
npx eas build --platform android --profile development
```

Install the resulting APK, open the app, go to the Map tab — you should see real Google
Maps tiles with your location and nearby incident pins.

## Troubleshooting

- **Map shows a grey box with "For development purposes only" watermark**: billing isn't enabled on the project (step 2), or the key/API isn't restricted correctly.
- **Map is blank/tiles never load**: double check the package name and SHA-1 in step 5 match exactly — a mismatch fails silently rather than showing an error.
- **Works in one build but not another**: you likely rebuilt with a different profile/keystore — repeat step 4 for that keystore's SHA-1 and add it to the same key's restrictions (a key can have multiple SHA-1 entries for the same package name).
