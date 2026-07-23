using UnityEngine;

namespace CaseGame.Environment
{
    /// <summary>
    /// Pure sizing math for <see cref="TerrainCompositor"/>'s runtime bake: picks a pixel
    /// resolution for the baked terrain texture that preserves the source tileset's native pixel
    /// density up to a hard dimension ceiling (bounded VRAM regardless of grid size), and always
    /// matches the world-space bounds' aspect ratio exactly — so a Sprite built from the result at
    /// the returned <c>pixelsPerUnit</c> covers precisely those bounds, no stretch or drift.
    /// </summary>
    public static class TerrainBakeResolution
    {
        public static (int pixelWidth, int pixelHeight, float pixelsPerUnit) Compute(
            float worldWidth, float worldHeight, float nativePixelsPerUnit, int maxDimension)
        {
            var nativeWidth = worldWidth * nativePixelsPerUnit;
            var nativeHeight = worldHeight * nativePixelsPerUnit;
            var scale = Mathf.Min(1f, maxDimension / nativeWidth, maxDimension / nativeHeight);

            var pixelsPerUnit = nativePixelsPerUnit * scale;
            var pixelWidth = Mathf.Max(1, Mathf.RoundToInt(worldWidth * pixelsPerUnit));
            var pixelHeight = Mathf.Max(1, Mathf.RoundToInt(worldHeight * pixelsPerUnit));

            return (pixelWidth, pixelHeight, pixelsPerUnit);
        }
    }
}
