# AGENTS.md: Larje Project Setup

Instructions for coding agents working on this package.

## Package layout

- `Editor/`: `Larje.ProjectSetup.Editor` (editor only, not auto-referenced).
  - `ProjectSetupRunner`: menu and batch entry points, step order, exit codes.
  - `ProjectSetupOptions`, `ProjectSetupContext` (asset paths), `ProjectSetupReport`.
  - `Steps/`: one `IProjectSetupStep` per area.
  - `Utility/SetupAssets`: load-or-create helpers and `SerializedObject` setters.
  - `Templates~/`: templates for generated project files, with `{{Namespace}}` and `{{ProductName}}` tokens.
  - `Build/`: `ProjectBuildRunner` and `ProjectBuildOptions` (batch builds).
- `Playtest/`: `Larje.ProjectSetup.Playtest` (any platform, `UNITY_EDITOR` constraint, namespace `Larje.ProjectSetup.Editor.Playtest` kept for compatibility): `PlaytestSession`, `PlaytestCapture`, `PlaytestInput` used by generated PlayMode playtests. They are public API of generated projects: keep them backward compatible and free of `UnityEditor` APIs. Do not move them back into the editor assembly: PlayMode test assemblies cannot reference editor-only assemblies.
- `WebGLTemplates~/`: WebGL templates copied into projects on demand (`LarjeTelegram`).

## Rules for steps

- Every step must be idempotent and must keep the user's changes. Load existing assets first and never overwrite files. Fill only empty references (`SetupAssets.SetReferenceIfMissing`), add only missing list entries (`SetupAssets.AddReferences`), and write plain values only when the asset was just created (`LoadOrCreate(path, out bool created)`). Never remove or reorder entries the user added, for example extra installers in `ProjectContext`.
- Create and change assets only through the Unity API (`AssetDatabase`, `PrefabUtility`, `EditorSceneManager`, `SerializedObject`), never as text.
- Set private serialized fields through `SetupAssets` using the field name. If a field is renamed in another package, update it here.
- Types from optional packages, or from code generated in the same run, are resolved with `Type.GetType`. When such a type is missing, report `Pending` so the runner exits with code 2 and is run again.
- Report every meaningful result through `context.Report`: `Info`, `Warning` for things the user should know, `Error` for things that block the base.

## Code style (C#)

- Do not use `var`. Write explicit types.
- Do not write comments in code.
- Do not use `continue`. Restructure the loop or extract a method.
- Seal classes when they are not designed for inheritance.
- Use block bodies `{ }` for methods and `if` statements. Expression-bodied members are fine for simple properties when the surrounding code already uses them.
- Do not name variables `value` except in property setters.
- Make private methods `static` only when a static context requires it.
- Order members as: constants, static fields, serialized fields, fields, properties, events, methods, nested enums, nested classes. Within each group order by access: public, protected, internal, private.
- Private non-serialized fields use `_camelCase`, constants use `UPPER_SNAKE_CASE`. `[SerializeField]` fields follow the style already used in the package (mostly `camelCase`); never rename an existing serialized field without `[FormerlySerializedAs]`, because assets and `com.larje.projectsetup` reference fields by name.
- Serialized fields: never use `[Tooltip]`. Group related fields with `[Header]` and avoid single-field headers.

## Architecture rules

- Services are plain C# classes with constructor injection. Do not add a service locator, singletons or a custom DI container.
- Use UniTask with a `CancellationToken` for loops, delays and async work. Do not use `Update` loops or coroutines in services.
- Ids are ScriptableObject assets. Persistent data lives in save sections owned by the feature.
- Zenject code goes only into the `*.Zenject` assembly. It compiles under `LARJE_ZENJECT`, which comes from `versionDefines` on `com.larje.zenject`.
- Unity AI MCP tools go only into the `*.AI.Editor` assembly. It compiles under `LARJE_UNITY_AI`. Tool names use the `Larje.` prefix and the `larje` group. Put logic that tests need into the regular editor assembly so the tests do not depend on Unity AI.
- Runtime assemblies must not reference editor-only APIs outside `#if UNITY_EDITOR`.

## Working in a Unity project

- Do not edit `.prefab`, `.unity`, `.asset` or `.mat` files as text. Change them through the Unity Editor or MCP tools.
- Commit the `.meta` file for every new file and folder. Git packages are immutable, so missing metas break consumers.
- After every C# change, refresh the editor, wait until compilation is idle, and check that the console has no errors or warnings before moving on.
- Run EditMode tests through the Test Runner. List the package in the project manifest `testables` first.

## Commits

- Write short technical commit messages only. Do not push unless explicitly asked.
