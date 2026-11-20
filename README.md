# Echo Shift

A turn-based grid puzzle game for Android, played in portrait with one thumb. Every swipe moves you one tile. Tap **Echo** and your past self replays those exact moves while you try a new route, so each room is solved by cooperating with your own past.

This repository holds the complete Unity project: the game rules, 40 levels across two worlds, every screen from the design docs, and Android build tools. It follows the *Echo Shift Project Docs* (Draft 3).

## What you need

You don't need Android Studio. Unity Hub installs everything required for Android builds.

1. **Unity Hub**, from unity.com/download.
2. **Unity 6 LTS** (6000.0.x), installed through Unity Hub with the **Android Build Support** module ticked, including **OpenJDK** and **Android SDK & NDK Tools**.
3. **Node 18 or later**, only if you want to run the level checker.
4. An Android phone with **Developer options** and **USB debugging** turned on, for testing on a device.

The project lists Unity 6000.0.23f1. If Unity Hub offers a newer 6000.0.x version, accept it.

## First run

1. Clone the repository.
2. In Unity Hub, choose **Add > Add project from disk** and pick the cloned folder.
3. Open it. The first import takes a few minutes.
4. If Unity asks to enable the new Input System backend and restart, choose **Yes**. The game works with either input setting.
5. The project sets itself up on first open. If it doesn't, run **Echo Shift > 1. Set Up Project** from the menu bar. This sets portrait mode, the package name and Android options, and creates `Assets/Scenes/Boot.unity`.
6. In the **Game** view, pick a portrait size such as 1080x1920. Add one with the **+** button if it's missing.
7. Press **Play**.

### Playing in the editor

| Input | Action |
|---|---|
| Arrow keys or WASD | Move |
| Space | Wait |
| Z | Undo |
| E | Echo |
| R | Restart |
| Esc | Android back |
| Mouse drag on the board | Swipe |

In the editor, rewarded ads and purchases succeed instantly with no real SDK, so you can test the whole hint and Shop flow.

## Running on your phone

1. Connect the phone by USB and accept the debugging prompt on the phone.
2. Choose **File > Build Profiles**, select **Android**, and click **Switch Platform**. This only has to be done once.
3. Click **Build And Run**. Alternatively, choose **Echo Shift > 2. Build Android APK (test)**, which writes `Builds/EchoShift.apk`. Copy it to the phone and install it.

## Project layout

```
Assets/
  Resources/Levels/      40 level files (w1_l01..w1_l20, w2_l01..w2_l20)
  Scripts/Simulation/    the puzzle rules in plain C#, no Unity types
  Scripts/Game/
    Core/                start-up, saving, level list, hints, text table, input
    Services/            ads, purchases, analytics, sound, vibration, review
    UI/                  design tokens, shape drawing, layout helpers, navigation
    Board/               board drawing and swipe input
    Screens/             every screen and popup
  Editor/                project setup, Android builds, level check
docs/LEVELS.md           how to write levels
tests/                   C# tests for the rules and every level
tools/levelcheck.mjs     level checker that runs with Node
.github/workflows/       runs both checks on every push
```

The game builds all of its UI from code when it starts, in `GameApp`. The scene stays empty, and there are no prefabs to keep in sync.

## Adding levels (monthly updates)

Add text files to `Assets/Resources/Levels/` and give each the next `number`. No code changes are needed. Check them before committing:

```
node tools/levelcheck.mjs
```

The full format is in [docs/LEVELS.md](docs/LEVELS.md). GitHub runs the same checks in C# on every push.

## Tests

- **On GitHub:** every push runs the level checker and the C# tests. They compile the real simulation code and prove every level's saved solution wins with three stars. See the **Actions** tab.
- **Locally:** run `node tools/levelcheck.mjs`, or `dotnet test tests/EchoShift.Simulation.Tests` if you have the .NET 8 SDK installed.
- **In Unity:** **Echo Shift > Check All Levels**.

## Before release

The game runs without any SDKs, using built-in test versions of ads, purchases and analytics. Before publishing:

1. **Names.** Set `StudioName`, `PackageId`, the privacy URL and the terms URL in `Assets/Scripts/Game/Core/GameConfig.cs`, then run **Echo Shift > 1. Set Up Project** again.
2. **SDKs.** Follow [docs/SDK_SETUP.md](docs/SDK_SETUP.md) to add AdMob, Unity IAP, Firebase and Play In-App Review. Each one is switched on with a scripting define.
3. **Signing.** Create an upload keystore under **Project Settings > Player > Android > Publishing Settings**. Keep two backups, and never commit it.
4. **Release build.** Run **Echo Shift > 3. Build Android App Bundle (release)**, which writes `Builds/EchoShift.aab`. Upload it to Play Console.

## Choices that differ from the project docs

These keep the project opening cleanly with no extra setup. Each one is easy to change later.

| Docs say | This project does | Why |
|---|---|---|
| TextMeshPro with Exo 2 and Nunito | Unity's built-in font through UI Text | No font import step. Swap in TextMeshPro and the real fonts with the final art. |
| Unity Localization package | One English string table in `Strings.cs` | Same idea, with keys ready for translation and no package setup |
| URP 2D renderer | Built-in renderer | The game is all UI, so URP adds nothing yet |
| Music and sound files | Synthesised placeholder sound effects, no music | No audio files yet. Replace them with `AudioService.Override`. |
| Volume sliders | On and off toggles | Simpler first version |
| Final art | Clean generated shapes in the design system colours | Placeholder until the art is commissioned |
