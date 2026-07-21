**2026-07-21 — SceneReference Asset Picker**

## 1. Summary
Replaced `GameManager`'s raw `string firstSceneName` field with a reusable `SceneReference`
wrapper that shows a proper Scene-asset picker in the Inspector instead of a typo-prone,
rename-unsafe string. `SceneAsset` only exists in the `UnityEditor` namespace (unavailable to
Player builds), so `SceneReference` holds an editor-only `SceneAsset` field guarded by
`#if UNITY_EDITOR`, and syncs it into a plain `sceneName` string via
`ISerializationCallbackReceiver.OnBeforeSerialize` — that string is the only thing actually
serialized into a build and is what `GameManager` reads at runtime. A `SceneReferenceDrawer`
(the project's first `Scripts/Editor/` script, first `CaseGame.Editor` asmdef) collapses the
Inspector display back down to a single picker field rather than exposing both the asset and
the synced string. Per the human's direction, this is meant as a general project convention
going forward — prefer typed asset-picker fields over raw strings wherever Unity doesn't
already provide one — recorded in `CONVENTIONS.md`'s overrides table for future features.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Core/SceneReference.cs`](Assets/_Project/Scripts/Runtime/Core/SceneReference.cs) — new reusable `[Serializable]` wrapper: editor-only `SceneAsset` field, runtime-safe `SceneName`/`IsSet`, syncs on serialize.
- [`Assets/_Project/Scripts/Runtime/Core/GameManager.cs`](Assets/_Project/Scripts/Runtime/Core/GameManager.cs) — `firstSceneName` (string) → `firstScene` (`SceneReference`); `Start()` updated accordingly.
- [`Assets/_Project/Scripts/Editor/CaseGame.Editor.asmdef`](Assets/_Project/Scripts/Editor/CaseGame.Editor.asmdef) — new Editor assembly (references `CaseGame.Runtime`, Editor-only platform).
- [`Assets/_Project/Scripts/Editor/SceneReferenceDrawer.cs`](Assets/_Project/Scripts/Editor/SceneReferenceDrawer.cs) — `CustomPropertyDrawer` for `SceneReference`, reusable tooling per CLAUDE.md's editor script policy (not a one-off).
- [`Docs/Agent/CONVENTIONS.md`](Docs/Agent/CONVENTIONS.md) — new override row recording the asset-picker-over-string convention.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — implementation log entry, decisions log entry #13.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode A — `dotnet build`, editor open
throughout): **passed**, 0 errors, 0 warnings, after confirming the generated `.csproj` files
actually picked up the new files (`SceneReference.cs` in `CaseGame.Runtime.csproj`,
`SceneReferenceDrawer.cs` in the newly-generated `CaseGame.Editor.csproj`) — the first attempt
raced ahead of Unity's asset reimport and had to be re-run once the editor caught up.

No automated tests: `SceneReference`'s only real logic is a null/empty check, and the
asset-picker sync behavior is inherently an Editor-GUI/serialization concern, not testable
without driving the Inspector. Hand-test instead (checklist below).

## 4. Editor hookup checklist
1. Open the project in Unity `2021.3.45f2` (or let it finish reimporting if already open).
2. Open `Boot.unity`, select the `GameManager` object under `--- SYSTEMS ---`.
3. In the Inspector, the field that used to be a text box labeled "First Scene Name" is now
   labeled **First Scene** and shows a Scene-asset picker (like a Prefab/Texture field).
4. Confirm it's still unassigned (blank) — expected, since `MainMenu.unity` doesn't exist yet.
   Nothing to assign right now; this is just confirming the picker renders correctly and the
   field didn't silently retain stale data from the old string field (it can't — the
   underlying serialized field name/type both changed).
5. No further action needed until the Main Menu feature lands and `MainMenu.unity` exists to
   drag into this field.

## 5. Deviations
None — this directly implements the human's explicit instruction ("Implement that so we don't
use String fields where we can use asset picker serializefields").
