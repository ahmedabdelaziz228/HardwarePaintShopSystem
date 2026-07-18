# Hardware Paint Shop Mobile

Flutter Android companion for the local shop API. It supports Arabic RTL UI, login, dashboard, product/name/barcode/serial search, customer statements and collections, low-stock alerts, product creation with camera image, stock adjustments, local cache, and queued uploads.

## Architecture

The app uses feature-first clean architecture. `lib` has only two architectural roots: `core` for app-wide infrastructure and `features` for business capabilities. Every feature owns its `data`, `logic`, and `ui` layers.

```text
lib/
├── main.dart
├── core/
│   ├── app/          # composition root and application widget
│   ├── config/       # runtime configuration
│   ├── errors/       # typed exceptions and error mapping
│   ├── network/      # the shared Dio client
│   ├── state/        # reusable state primitives
│   ├── storage/      # SQLite cache and offline queue
│   ├── theme/        # Material 3 design tokens
│   ├── utils/        # parsing, formatting, and identifiers
│   └── widgets/      # widgets reused by multiple features
└── features/
    ├── auth/
    ├── dashboard/
    ├── products/
    ├── customers/
    ├── alerts/
    ├── sync/
    └── shell/
        ├── data/     # models and repositories when required
        ├── logic/    # Cubits and immutable states
        └── ui/       # pages and feature-specific widgets
```

State is managed by `flutter_bloc` Cubits. UI reads state and dispatches intentions only; repositories own data access, and `ApiClient` is the only HTTP boundary. Networking uses Dio with centralized timeout, HTTP, authentication, and connectivity error mapping. Asynchronous boundaries use `try/catch`, expose loading/error/success states, and do not silently swallow exceptions.

## Generate platform files once

Install a current stable Flutter SDK, then run from this folder:

```powershell
flutter create --platforms=android --org com.hardwarepaintshop .
flutter pub get
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
```

The checked-in Android manifest enables camera, LAN internet, and clear-text HTTP for the local `http://192.168.x.x:5000` API. After `flutter create`, verify the manifest still contains those permissions.

## Run

Enable Developer Options and USB debugging on the Android phone:

```powershell
flutter devices
flutter run
```

Use the laptop IPv4 address in the login screen, for example:

```text
http://192.168.1.10:5000
```

Never use `localhost` on the phone. The laptop and phone must be on the same trusted Wi-Fi network. The Windows firewall must allow TCP port 5000 on Private networks.

## Release APK

Configure an Android signing key before customer delivery, then:

```powershell
flutter build apk --release
```

The unsigned/debug application is for acceptance testing only.

## Quality checks

Run these before opening a pull request:

```powershell
dart format --output=none --set-exit-if-changed lib test
flutter analyze
flutter test
flutter build apk --debug
```

GitHub Actions runs the same checks and publishes the debug APK as a workflow artifact.
