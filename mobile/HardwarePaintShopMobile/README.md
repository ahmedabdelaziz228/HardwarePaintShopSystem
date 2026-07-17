# Hardware Paint Shop Mobile

Flutter Android companion for the local shop API. It supports Arabic RTL UI, login, dashboard, product/name/barcode/serial search, customer statements and collections, low-stock alerts, product creation with camera image, stock adjustments, local cache, and queued uploads.

## Generate platform files once

Install a current stable Flutter SDK, then run from this folder:

```powershell
flutter create --platforms=android --org com.hardwarepaintshop .
flutter pub get
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
