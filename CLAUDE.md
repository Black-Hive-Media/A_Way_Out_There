# A Way Out There

Casual, family-friendly, no-combat 2D top-down space salvage and trading game.
1–4 players in local split-screen, at most 2 per ship (Pilot + Co-pilot).
Targets: PC, Nintendo Switch, Xbox. Local multiplayer only for now.

The full design lives in `Docs/DataDesign.md`. Read it before any data, economy,
upgrade, docking, dialog or community-goal work. Its "Code changes, in order"
section is the build plan; do one step at a time.

## Project

- Unity 6 (6000.0.18f1), URP 2D, C#.
- Packages in use: Input System (PlayerInput, action maps "Player" and "UI"),
  Cinemachine 3, TextMeshPro, uGUI, 2D physics.
- Main scene: `Assets/Scenes/GameScene.unity`.
- Our code: `Assets/Scripts/` (MonoBehaviours), `Assets/Scripts/CollectiblesScripts/`
  (item subclasses), `Assets/ScriptableObjects/` (SO classes and their assets).
- Third-party, do not modify: `Assets/JMO Assets/`, `Assets/Plugins/`,
  `Assets/TextMesh Pro/`.

## Conventions

- Every piece of content has a stable lowercase string ID with a type prefix:
  `sys_sol`, `port_sol_refinery`, `ind_mining`, `itm_gold`, `var_gold_pristine`,
  `upg_laser_t2`, `npc_hauler`, `dlg_cargo_full`, `goal_food_shortage`.
  Never rename an ID once it exists; save data refers to content only by ID.
- Content data = ScriptableObjects generated from CSV in `Assets/Data/Source/`
  by an Editor importer into `Assets/Data/Content/`. Do not hand-edit generated
  assets when a CSV row exists for them.
- Save data = plain C# classes serialized to JSON through `ISaveStorage`.
  No SQLite, no PlayerPrefs for game state, no ScriptableObjects for runtime state.
- Editor-only code goes in an `Editor` folder or behind `#if UNITY_EDITOR`.
- Keep the game family-friendly: nothing is ever destroyed, deliveries never fail,
  dialog stays kind (the "Cheeky" tone is playful, never mean).

## Rules for changes

- Work on a feature branch, never directly on `main`. Commit at the end of each
  plan step with a message naming the step.
- Never edit or delete `.meta` files, and never delete assets, prefabs or scenes
  unless I ask. Unity owns `.meta` files.
- Do not edit `.unity` scene or `.prefab` files by hand; tell me what to change
  in the Editor instead.
- When changing a serialized field on a ScriptableObject or MonoBehaviour, keep
  existing data safe: add the new field, migrate with an Editor script, and only
  remove the old field after I confirm the migration in Unity.
- Do not add packages without asking. Anything added must support PC, Switch and Xbox.
- You cannot run the Unity Editor. After each change, list what I should check in
  Unity (compile errors, Inspector fields, a play-test), then wait for my results.