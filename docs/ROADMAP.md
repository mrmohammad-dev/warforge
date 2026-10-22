# WarForge roadmap

## Milestone 0.1 (this archive): vertical slice, unverified
- [ ] Open in Unity 6000.0.23f1, run WarForge > Setup Project, fix any compile errors reported in the Console
- [ ] Run EditMode tests and make them green
- [ ] Play in Editor: drive, turret aim, third person, gunner sight, fire, hit a target, win/lose a mission
- [ ] Build debug APK, sideload, test touch controls on a real phone (stick, aim drag, buttons)
- [ ] Verify URP materials render on device; add shaders to Always Included if needed

## Milestone 0.2: feel and physics
- [ ] Tune suspension, track grip, pivot turns, and hill climbing on the arena terrain
- [ ] Gun stabilisation toggle and turret lag tuning
- [ ] Ballistic drop display on HUD (range and lead marker)
- [ ] Ammo rack in turret, separate shell stocks, shell switching penalty

## Milestone 0.3: damage and AI
- [ ] Module damage visualisation (crew injured, engine smoke, track damage model)
- [ ] Enemy AI: cover use, hull-down behaviour, retreat at low HP, team coordination
- [ ] Spaced armor and slope test range for practice

## Milestone 0.4: content
- [ ] Authored vehicle models (mesh import), turret and barrel rigs, camouflage textures
- [ ] Second map (forest) and third map (town) using the same ArenaBuilder pattern
- [ ] Campaign with 10+ missions, garage upgrades with credits

## Milestone 0.5: polish and performance
- [ ] Authored audio, engine loops per vehicle, reverb per map
- [ ] uGUI menus, garage vehicle previews
- [ ] Mobile profiling: draw calls, particle budgets, quality presets
- [ ] Aircraft architecture (new VehicleClass and flight controller) when the ground game is solid
