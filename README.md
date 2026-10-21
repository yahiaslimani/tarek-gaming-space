# Tarek Gaming Space

A floating timer strip for a gaming room. Each poste (console seat) gets its own block on the desktop. A session is either **Open** (stopwatch) or **Timer** (fixed time), and the block shows the price in DZD as the client plays.

Runs on Windows 10 and 11, 32-bit and 64-bit (.NET Framework 4.8, which Windows 10 already includes).

## Features

- One always-on-top strip with 1–5 postes. Poste 1 sits on the right and new postes attach on the left. Drag the name bar to move the strip.
- Open or Timer sessions, with pause, +15 min, and switching between modes. Open → Timer can either add the minutes on top of the time played or count them from the start.
- Price = time played × hourly rate, always rounded down to the previous 10 DZD. Paused time is free, and a client who leaves early pays only for the time played.
- The block turns orange in the last minutes. When time is up it turns red and blinks, a sound loops until you click the block, and Windows shows a notification.
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
