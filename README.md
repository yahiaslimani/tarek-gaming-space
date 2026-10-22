# Tarek Gaming Space

A floating timer strip for a gaming room. Each poste (console seat) gets its own block on the desktop. A session is either **Open** (stopwatch) or **Timer** (fixed time), and the block shows the price in DZD as the client plays.

Runs on Windows 7 SP1, 8, 8.1, 10 and 11, 32-bit and 64-bit (.NET Framework 4.5, which Windows 8 and later already include).

## Download

**[⬇ Download TarekGamingSpace-Setup.exe](https://github.com/yahiaslimani/tarek-gaming-space/releases/latest/download/TarekGamingSpace-Setup.exe)** (latest version, about 2 MB)

All versions and change notes are on the [Releases page](https://github.com/yahiaslimani/tarek-gaming-space/releases).

### Install

1. Run `TarekGamingSpace-Setup.exe`. No admin rights are needed.
2. If Windows shows "Windows protected your PC", click **More info → Run anyway**. The app isn't code-signed, so Windows doesn't recognise it yet.
3. Finish the setup. The strip appears at the top right of the screen, and the app starts with Windows from then on.

To update, run the new setup over the old one. Settings and running sessions are kept. To uninstall, use Windows Settings → Apps.

## Features

- One always-on-top strip with 1–5 postes. Poste 1 sits on the right and new postes attach on the left. Drag the name bar to move the strip.
- Open or Timer sessions, with pause, adding any number of minutes the client asks for, and switching between modes. Open → Timer can either add the minutes on top of the time played or count them from the start.
- Price = time played × hourly rate, always rounded down to the previous 10 DZD. Paused time is free, and a client who leaves early pays only for the time played.
- The block turns orange in the last minutes. When time is up it turns red and blinks, a sound loops until you click the block (the sound can be switched off), and Windows shows a notification.
- Adjustable opacity from the ◐ button on the strip (20–100%). By default only the dark background fades, so the numbers stay sharp. The strip goes solid under the mouse and when a poste is in its last minutes or out of time.
- English and Arabic (right-to-left), start with Windows, hide/show from the tray.
- Sessions are stored as timestamps in `%AppData%\TarekGamingSpace\state.json`, so they continue correctly after a restart or crash.

## Build

Requirements: a .NET SDK (6 or later) and, for the installer, [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

This runs the tests, publishes the app to `artifacts\app` and builds `installer\Output\TarekGamingSpace-Setup-<version>.exe`.

For development runs that shouldn't touch your real settings, set `TGS_DATA_DIR` to another folder before starting the app.

## Project layout

- `src/TarekGamingSpace/Core/SessionMath.cs`: all time and price rules (unit-tested).
- `src/TarekGamingSpace/Core/Loc.cs`: English and Arabic strings.
- `src/TarekGamingSpace/ViewModels`: the app and poste state.
- `src/TarekGamingSpace/Views`: the strip, poste block, dialogs and control panel.
- `src/TarekGamingSpace/Services`: storage, sounds, tray icon, start with Windows.
- `installer/`: Inno Setup script.

Fonts: Chakra Petch and Share Tech Mono, under the SIL Open Font License (licence files are in `Assets/Fonts`).
