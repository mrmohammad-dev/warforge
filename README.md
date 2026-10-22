# WarForge

Offline, single-player 3D armored combat for Android (landscape). Built in **Unity 6000.0.23f1 (Unity 6), URP 17, C#**.
Original vehicles, names and assets only. No accounts, no network calls, no servers.

## Status (read this first)

This is **milestone 0.1, a vertical-slice codebase**. The files were written without access to a Unity install,
so the following are **NOT verified yet**:

- Compilation (not verified; treat actual Unity Console errors as the source of truth)
- Runtime behaviour in the Editor or on a phone
- APK build
- Touch controls on a real device
- EditMode tests (`Assets/WarForge/Tests/EditMode`)

Treat the first Unity open as the real test.

## Setup on your phone workflow

1. Merge this archive into the root of `mrmohammad-dev/warforge` on GitHub (Assets, Packages, ProjectSettings, README, .gitignore, and `docs/`). If the repository already contains `AGENTS.md` or `docs/PROTOTYPE.md`, keep those existing files.
2. Open the project in Unity Hub (Unity 6000.0.23f1 with Android Build Support, on a PC or Mac). Phones cannot run Unity Editor.
3. Menu **WarForge > Setup Project** (creates the main scene, player settings, Android target).
4. URP asset (one-time, only if the setup log asks for it): right-click `Assets/WarForge/Settings` > Create > Rendering > URP Asset (with Universal Renderer). Drag it into Project Settings > Graphics and Project Settings > Quality.
5. Run **Edit > Project Settings > Player > Other Settings > Active Input Handling = Input Manager (Old)** if the Setup log warns about it. Touch controls use the legacy input API.
6. Test in Editor: Play. WASD drive, mouse right-button drag to aim, Space or left click fire, V gunner sight, Q change shell, Esc pause.
7. Build: **WarForge > Build Android Debug APK** (output in `Builds/`). Sideload the APK on your phone.
8. Run tests: Window > General > Test Runner > EditMode.

## Controls (touch)

- Left thumb: virtual stick. Up/down = throttle, left/right = steer (track differential, pivot turns allowed).
- Right thumb drag: orbit camera (third person) or move the sight (gunner view).
- FIRE, VIEW, SHELL buttons bottom right. II top left = pause.

## Project layout

```
Assets/WarForge/
  Scripts/
    Core/        GameRoot (flow), GameBootstrap, SaveSystem (local JSON), SettingsApplier
    Data/        VehicleSpec, VehicleCatalog, ShellSpec (data-driven, add vehicles here)
    Vehicles/    Vehicle, VehicleFactory (builds tanks), TankController (tracked driving), TurretController, TankAudio
    Combat/      ArmorMath, BallisticsMath (pure, tested), Projectile, TankGun, DamageModel (modules, fire, ammo), ArmorPlate
    Cameras/     VehicleCameraRig (third person + gunner sight)
    Controls/    TouchControls (touch + keyboard/mouse intent)
    AI/          EnemyTankAI (patrol, flank, lead-aim)
    Missions/    MissionCatalog (data), MissionTracker (win/lose logic)
    World/       ArenaBuilder (procedural terrain/village/forest), Destructible, TreeProp
    FX/          EffectsManager, AudioManager (procedural placeholder sound), MaterialLibrary
    UI/          GameRoot.UI (IMGUI menus, HUD, pause, results)
  Editor/        WarForgeProjectSetup (setup + Android builds)
  Tests/EditMode/  unit tests for armor, ballistics, data and saves
Docs/ROADMAP.md  next concrete tasks
```

## Adding a vehicle or mission

- Vehicle: add a `Light/Medium/Heavy(...)` entry in `Data/VehicleCatalog.cs`. Use the `unlockedByMission` argument to lock it.
- Mission: add a `MissionDefinition` in `Missions/MissionCatalog.cs`. Enemies are `EnemySpawn` entries with arena-relative positions.

## Known limitations of this milestone

- Models are primitives (boxes and cylinders) that are generated in code. Authored meshes plug into `VehicleFactory`.
- Sounds are generated procedurally. Authored audio plugs into `AudioManager`.
- UI uses IMGUI for speed. It should move to uGUI prefabs before polish.
- Materials are created at runtime from the URP Lit shader. If materials show pink on device, add the URP Lit shader to Project Settings > Graphics > Always Included Shaders.
