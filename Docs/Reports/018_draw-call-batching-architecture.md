**2026-07-22 — Draw-call/batching architecture**

## 1. Summary
You asked when to do the draw-call/batching pass — before or after visual polish — and we
agreed to split it: batching *architecture* now, numeric *verification* (actually counting
draw calls against the brief's &lt;20 budget) once visual polish is far enough along that the
numbers mean something. This report is the architecture half only.

I audited the actual current state rather than assuming anything needed fixing:
- **SRP Batcher** (`Assets/Settings/UniversalRP.asset`, `m_UseSRPBatcher: 1`) was already
  enabled — this is URP's project-template default, no action needed.
- **Material sharing** — every building/unit prefab's `Visuals` `SpriteRenderer` already uses
  the same material (URP's default sprite material). This is the other precondition for
  batching, and it was already correct without me touching anything.
- **The actual gap:** no `SpriteAtlas` existed. Unity's sprite batching requires matching
  *texture*, not just matching material — so even though every prefab shares one material,
  a Barracks and a Soldier (different source textures — you've since imported the "Tiny Swords"
  pack for real art) would still cost separate draw calls next to each other on screen. Fixed by
  adding `SpriteAtlas_Gameplay.spriteatlas` covering the actual art folders (`Blue Buildings`/
  `Blue Units`), so same-material sprites across different building/unit types can now batch.
- **GPU Instancing** — the brief names this as its own required pattern, separate from
  batching. The default URP sprite material is package-shipped (I don't control or want to
  modify it) and already ships instancing-capable. The project's own custom material,
  `M_SpriteGrayscaleGhost` (the placement-ghost shader, Report 013), had it **off**. Enabled it.

No C# code was touched this feature — this was entirely asset/settings configuration, done via
a throwaway editor script (Sprite Atlas packable/packing-setting assignment is fiddly enough by
hand to clear CLAUDE.md's exception threshold) and verified by reading the generated
`.spriteatlas`/`.mat` files back rather than trusting a clean exit code.

**What this doesn't do:** measure actual draw calls, touch the Frame Debugger/Stats window, or
guarantee &lt;20 — that's the deferred verification pass, deliberately, per our discussion.

## 2. Changes
**New/modified assets** (via throwaway `Scripts/Editor/Setup/Temp/DrawCallArchitectureSetup.cs`,
deleted after use):
- `Assets/_Project/Art/Textures/SpriteAtlas_Gameplay.spriteatlas` — new. Packables: `Assets/Tiny
  Swords/Buildings/Blue Buildings/` and `Assets/Tiny Swords/Units/Blue Units/` (whole folders,
  not just the 5 currently-referenced sprites — see decisions log #50 for why). Texture
  settings: Point filtering (matches the source art's pixel style), no mip-maps, 4px padding, no
  rotation/tight-packing (kept simple/predictable). Packed and verified — 145 sprite references
  in the saved atlas file.
- `Assets/_Project/Art/Materials/M_SpriteGrayscaleGhost.mat` — `m_EnableInstancingVariants`
  0 → 1 (GPU Instancing enabled).
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — implementation log entry, decisions log #49–50, scene/asset inventory note (also documents the `Tiny Swords/` third-party folder for evaluator clarity, since I encountered it while auditing).
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

## 3. Test results
Compile check (Mode A `dotnet build`, editor closed): **passed** — 0 `error CS` lines. (No C#
was added or modified, so this just confirms the throwaway script itself compiled and nothing
regressed — the meaningful verification for *this* feature is the asset-file read-back below,
not test count.)

EditMode tests (Mode B batchmode): **127/127 passed** — unchanged from Report 017, as expected
(no runtime code touched).

## 4. Audit checklist
No manual hookup needed — a throwaway script did the work. What I verified myself, and what's
worth a 30-second human glance:

### Already verified (agent, reading the generated files back)
- `SpriteAtlas_Gameplay.spriteatlas`'s two packable entries resolve to the exact GUIDs of
  `Assets/Tiny Swords/Buildings/Blue Buildings` and `Assets/Tiny Swords/Units/Blue Units`
  (cross-checked against those folders' own `.meta` files, not just "a folder got added").
- The atlas's `m_PackedSprites` list is populated (145 references) — confirms packing actually
  ran and produced content, not just an empty configured-but-unpacked atlas.
- Texture settings in the saved file match what was requested: `filterMode: 0` (Point),
  `padding: 4`, `enableRotation: 0`, `enableTightPacking: 0`.
- `M_SpriteGrayscaleGhost.mat`'s `m_EnableInstancingVariants` reads `1` in the saved file.
- Compile clean, 127/127 tests, after the change.

### Worth a human glance (I can't visually inspect the Editor)
1. **Sprite Atlas Inspector → "Include in Build."** A newly-created `SpriteAtlas` defaults to
   included, and nothing in this project's settings should have changed that, but I can't see
   the Inspector checkbox directly (there's no corresponding YAML field I could grep to confirm
   it either way) — worth a glance before a Windows build is cut.
2. **Visual sanity check.** Select a building/soldier in Play Mode or the Scene view and confirm
   the sprite still looks correct (no atlas-packing artifacts — bleeding between packed sprites,
   wrong UVs, etc.). Atlas packing is generally safe with 4px padding, but I haven't seen it
   rendered.
3. **If you add new building/unit art during polish:** no action needed *if* it lands inside
   `Blue Buildings`/`Blue Units` (or their subfolders) — the atlas repacks automatically. If you
   add art somewhere else (a new top-level folder, a different color variant), add that folder
   to `SpriteAtlas_Gameplay`'s packables too, or the new sprites won't benefit from atlasing.

## 5. Deviations
None beyond the two decisions already explained in Summary (splitting architecture from
verification; packing whole folders rather than just the 5 currently-used sprites) — both
recorded in ARCHITECTURE.md's decisions log (#49, #50).
