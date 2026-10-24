# Larje Project Setup

`com.larje.projectsetup` is an editor-only bootstrapper. It turns an empty Unity 6 project into a working base on Larje packages: Zenject `ProjectContext`, core services and configs, input, URP, scenes, a UI screen and a project assembly with installers. It runs from a menu or in batch mode, and every step is idempotent, so re-running it only adds what is missing.

## Install

```json
"com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10",
"com.larje.core": "https://github.com/danil777lar/unity-gameplay-core-services.git",
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
| Project Code | `Scripts/<Namespace>.asmdef`, `ProjectInstaller`, `MenuScreenController`, plus `CLAUDE.md` in the project root |
| Project Context | `Resources/ProjectContext.prefab` with the core, character and project installers |
| Validation | Runs `IConfigValidatable` on the generated configs and reports problems as warnings |

All generated content lives under `Assets/_Project`. Existing files are never overwritten. Only references and installer lists are updated.

## Claude Code skill

The `unity-new-project` skill (`~/.claude/skills/unity-new-project`) wraps this package. It creates the project in batch mode, writes the manifest from a preset, runs setup until it is done, and makes the first git commit.
