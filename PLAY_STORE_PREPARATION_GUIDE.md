# Google Play Store Publishing & AdMob Setup Guide for WearCar

This guide provides step-by-step instructions to take **WearCar** ("Dude, Find My Car") from development to release on the Google Play Store with working Google Mobile Ads (AdMob).

---

## 1. Setting Up Google Mobile Ads (AdMob)

### Step 1: Create an AdMob Account & App Entry
1. Sign in to [Google AdMob Console](https://admob.google.com/).
2. Click **Apps** > **Add App**.
3. Select **Android** platform and specify whether your app is listed on a supported app store (Select **No** if not published yet).
4. Enter your App Name: **Dude, Find My Car**.
5. Copy your **AdMob App ID** (Format: `ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY`).

### Step 2: Create a Banner Ad Unit
1. Inside your AdMob app dashboard, go to **Ad units** > **Add ad unit**.
2. Select **Banner** ad format.
3. Enter Ad unit name: `Main_Bottom_Banner`.
4. Click **Create ad unit** and copy your **Banner Ad Unit ID** (Format: `ca-app-pub-XXXXXXXXXXXXXXXX/ZZZZZZZZZZ`).

### Step 3: Replace Test IDs in the Codebase

#### A. Update Android Manifest:
In [`Platforms/Android/AndroidManifest.xml`](file:///C:/Users/admin/source/repos/WereCar/WearCar/Platforms/Android/AndroidManifest.xml):
```xml
<!-- Replace ca-app-pub-3940256099942544~3347511713 with your real AdMob App ID -->
<meta-data android:name="com.google.android.gms.ads.APPLICATION_ID" android:value="YOUR_REAL_ADMOB_APP_ID_HERE" />
```

#### B. Update XAML Views:
In [`Views/CompassPage.xaml`](file:///C:/Users/admin/source/repos/WereCar/WearCar/Views/CompassPage.xaml) and [`Views/ParkedMapPage.xaml`](file:///C:/Users/admin/source/repos/WereCar/WearCar/Views/ParkedMapPage.xaml):
```xml
<!-- Replace ca-app-pub-3940256099942544/6300978111 with your real Banner Ad Unit ID -->
<controls:BannerAd AdUnitId="YOUR_REAL_BANNER_AD_UNIT_ID_HERE" ... />
```

---

## 2. Generating a Release Android Keystore

Google Play requires all uploaded Android App Bundles (`.aab`) to be digitally signed with a private release key.

Run the following command in PowerShell to generate a new signing key:

```powershell
keytool -genkey -v -keystore release.keystore -alias wearcar-key -keyalg RSA -keysize 2048 -validity 10000
```

> [!CAUTION]
> **Keep your keystore file and passwords safe!** If you lose this keystore file or forget the password, Google Play will not allow you to upload updates for your app.

---

## 3. Building the Release Android App Bundle (.aab)

To generate the production `.aab` package for Google Play:

```powershell
dotnet publish C:\Users\admin\source\repos\WereCar\WearCar\WearCar.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=release.keystore -p:AndroidSigningKeyAlias=wearcar-key -p:AndroidSigningKeyPass=YOUR_KEY_PASSWORD -p:AndroidSigningStorePass=YOUR_STORE_PASSWORD
```

The compiled package will be saved at:
`C:\Users\admin\source\repos\WereCar\WearCar\bin\Release\net10.0-android\publish\com.HolmesSoft.wearcar-Signed.aab`

---

## 4. Google Play Console Setup & Submission

1. Sign up for a [Google Play Developer Account](https://play.google.com/console) ($25 one-time registration fee).
2. Click **Create app** and provide the required details:
   - **App Name**: Dude, Find My Car
   - **Default Language**: English (US)
   - **App or Game**: App
   - **Free or Paid**: Free
3. **Data Safety Form**:
   - Declare that the app collects **Location data** (for automatic parking detection and map display).
   - Declare that **Advertising ID / Device IDs** are collected for advertising (AdMob).
4. **Background Location Permission Declaration**:
   - Google Play will require you to submit a brief video or explanation justifying `ACCESS_BACKGROUND_LOCATION`.
   - **Explanation to submit**: *"WearCar uses background location to automatically detect when a user finishes driving and parks their vehicle without needing to open the app manually."*
5. **Privacy Policy**:
   - Provide a public URL to your privacy policy (required because the app collects location data and displays ads).

---

## 5. Monetization Strategy Recommendations (Ads vs Paid)

### Why a **Hybrid / Freemium Model** outperforms both Pure Ads and Pure Paid:

| Strategy | Pros | Cons | Verdict |
| :--- | :--- | :--- | :--- |
| **Paid Upfront** ($0.99 - $2.99) | Every download generates immediate revenue. | Extremely high drop-off. Free alternatives dominate utility apps on Google Play. | Low total revenue for new apps. |
| **Ad-Supported** (Free) | Zero download friction; builds active user base quickly. | Utility apps have short user sessions (10-30 seconds), yielding modest banner revenue per user. | Good volume, moderate revenue. |
| **Hybrid / Freemium** (Recommended) | **Free with Ads + $1.99 - $2.99 "Remove Ads" In-App Purchase** | Requires simple In-App Billing setup. | **Highest revenue potential.** Captures both free ad impressions and paying users. |

### Recommended Next Steps for Monetization:
1. Launch with **Ad-Supported Free Model** to build downloads and gather analytics.
2. Add an optional **"Remove Ads" In-App Purchase** via `Plugin.InAppBilling` to allow satisfied users to upgrade for $1.99.
