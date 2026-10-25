# Larje Project Setup

`com.larje.projectsetup` is an editor-only bootstrapper. It turns an empty Unity 6 project into a working base on Larje packages: Zenject `ProjectContext`, core services and configs, input, URP, scenes, a UI screen and a project assembly with installers. It runs from a menu or in batch mode, and every step is idempotent, so re-running it only adds what is missing.

## Install

```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10",
"com.larje.core": "https://github.com/danil777lar/unity-gameplay-core-services.git",
"com.larje.ui": "https://github.com/danil777lar/unity-ui-framework.git",
"com.larje.zenject": "https://github.com/danil777lar/unity-zenject-upm.git#9.2.0",
"com.larje.projectsetup": "https://github.com/danil777lar/unity-project-setup.git"
```

It also needs URP, Input System and uGUI from the Unity registry.

## Run

- **Menu**: `Tools/Larje/Project Setup/Run`. Options are read from `ProjectSettings/LarjeProjectSetup.json`. `Tools/Larje/Project Setup/Create Options File` writes the defaults.
- **Batch mode**:

```bash
Unity -batchmode -nographics -projectPath <project> \
  -executeMethod Larje.ProjectSetup.Editor.ProjectSetupRunner.RunFromCommandLine \
  -larjeSetupOptions <options.json> -logFile -
```

Exit codes: `0` done, `1` errors, `2` pending (the generated code must compile first, so run again). The report is written to `Logs/LarjeProjectSetup.txt`.

## Options

| Field | Default | Meaning |
|---|---|---|
| `ProductName`, `CompanyName` | `Game`, `Larje` | Player settings |
| `RootNamespace` | `Game` | Namespace and assembly name of the generated project code |
| `BundleId` | `com.<company>.<product>` | Application identifier for Android, iOS and Standalone |
| `Platform` | `Android` | `BuildTarget` name. The step is skipped with a warning if the module is not installed |
| `Orientation` | `Portrait` | `Portrait`, `Landscape` or `Auto` |
| `Character` | `false` | Adds `LarjeCharacterInstaller` and `CursorConfig` (requires `com.larje.character`) |
| `AssetStorePackages` | `[]` | `.unitypackage` names (or paths relative to the cache) to import from the Asset Store download cache |
| `AssetStoreCachePath` | OS default | Overrides the cache folder. The `ASSETSTORE_CACHE_PATH` environment variable also works |

## Steps

| Step | Result |
|---|---|
| Player Settings | Company, product, bundle id, orientation, linear color space, Input System handler, build target |
| Render Pipeline | `Settings/URP.asset` and its renderer, assigned to Graphics and every Quality level |
| Asset Store Import | Imports cached `.unitypackage` files once. They are tracked in `ProjectSettings/LarjeAssetStoreImports.txt` |
| Ids | Game states, sound channels, `MainMenu` screen, `Coins` currency, `Default` placement |
| Input | `Settings/GameInput.inputactions` from the Input System template, project-wide actions, `InputConfig`, `PointerControlConfig` |
| Configs | Save, GameStates, SceneLoader, AppFlow (Splash → Main), TimeScale, Sound, Currency, Items, Purchases, DebugConsole |
| UI | `Prefabs/UI/MainMenuScreen.prefab` listed in `UIConfig` |
| Scenes | `Scenes/Splash` and `Scenes/Main` with an EventSystem, added to Build Settings |
| Project Code | `Scripts/<Namespace>.asmdef`, `ProjectInstaller`, `MenuScreenController`, the smoke playtest in `Tests/PlayMode` (`<Namespace>.Playtests`), plus `CLAUDE.md` in the project root |
| Project Context | `Resources/ProjectContext.prefab` with the core, UI, character and project installers |
| Validation | Runs `IConfigValidatable` on the generated configs and reports problems as warnings |

All generated content lives under `Assets/_Project`. Existing files are never overwritten. Only references and installer lists are updated.

## Builds

`Larje.ProjectSetup.Editor.Build.ProjectBuildRunner.RunFromCommandLine` builds the enabled Build Settings scenes:

```bash
Unity -batchmode -nographics -projectPath <Project> -executeMethod Larje.ProjectSetup.Editor.Build.ProjectBuildRunner.RunFromCommandLine \
  -larjeBuildTarget Android [-larjeBuildAab] [-larjeBuildMono] [-larjeBuildDevelopment] [-larjeBuildVersion 1.0.0] [-larjeBuildNumber 3] \
  [-larjeKeystore path -larjeKeyAlias alias] [-larjeWebGLTemplate LarjeTelegram] [-larjeBuildOutput path]
```

- Android defaults to IL2CPP + ARM64; `-larjeBuildMono` uses Mono + ARMv7. Keystore passwords come from `LARJE_KEYSTORE_PASS` / `LARJE_KEY_PASS`; without a keystore the debug key is used.
- WebGL uses Gzip with decompression fallback. `-larjeWebGLTemplate LarjeTelegram` copies `WebGLTemplates~/LarjeTelegram` (Telegram WebApp `ready`/`expand`, Adsgram script) into `Assets/WebGLTemplates` and selects it.
- The summary is written to `Logs/LarjeBuild.txt`; exit code 0 on success.

## Playtests

`Playtest/` (assembly `Larje.ProjectSetup.Playtest`, compiled only in the editor through the `UNITY_EDITOR` constraint, never in players) contains helpers for PlayMode tests that run in the editor (also in batch mode with graphics). The generated `<Namespace>.Playtests` assembly references it and has no platform restriction: an assembly limited to the Editor platform is treated as EditMode tests and is skipped by PlayMode runs.

- `PlaytestSession`: scene loading and waiting, button lookup and clicks by name or label, screenshots, error collection, frame time sampling and `Logs/Playtest/<name>/report.txt`.
- `PlaytestCapture`: renders all cameras plus screen-space overlay canvases into a PNG (works without a Game view).
- `PlaytestInput`: virtual keyboard, mouse and touchscreen through Input System state events.

## Claude Code skills

`unity-build` and `unity-playtest` wrap the build runner and the playtests.



The `unity-new-project` skill (`~/.claude/skills/unity-new-project`) wraps this package. It creates the project in batch mode, writes the manifest from a preset, runs setup until it is done, and makes the first git commit.
