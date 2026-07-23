using CaseGame.Grid;
using UnityEngine;

namespace CaseGame.Environment
{
    /// <summary>
    /// Bakes the Water/Island_Cliff/Island_Grass Tilemaps into a single texture on one
    /// <see cref="outputRenderer"/> quad at gameplay-scene load, then hides the source Tilemaps.
    /// Tilemaps can't join the SRP Batcher no matter how they're configured, so folding them
    /// into one plain <see cref="SpriteRenderer"/> trades runtime terrain editing (not needed
    /// once a match starts) for a much cheaper draw. <see cref="TerrainBounds"/>/
    /// <see cref="TerrainBakeResolution"/> hold the sizing math; this class just orchestrates
    /// the camera/render-texture/sprite calls.
    /// </summary>
    public class TerrainCompositor : MonoBehaviour
    {
        [SerializeField] private Camera bakeCamera;
        [SerializeField] private Renderer[] sourceRenderers;
        [SerializeField] private SpriteRenderer outputRenderer;
        [SerializeField] private Material spriteMaterial;
        [SerializeField] private float nativePixelsPerUnit = 64f;
        [SerializeField] private int maxBakeDimension = 4096;
        [SerializeField] private float cameraDistance = 10f;

        public void Bake(GridModel grid, float terrainMargin)
        {
            var (min, max) = TerrainBounds.Compute(grid, terrainMargin);
            var size = max - min;
            var center = (min + max) * 0.5f;

            var (pixelWidth, pixelHeight, pixelsPerUnit) =
                TerrainBakeResolution.Compute(size.x, size.y, nativePixelsPerUnit, maxBakeDimension);

            var sprite = RenderToSprite(center, size, pixelWidth, pixelHeight, pixelsPerUnit);

            outputRenderer.sprite = sprite;
            outputRenderer.sharedMaterial = spriteMaterial;
            outputRenderer.transform.position =
                new Vector3(center.x, center.y, outputRenderer.transform.position.z);

            foreach (var sourceRenderer in sourceRenderers)
            {
                if (sourceRenderer != null)
                {
                    sourceRenderer.enabled = false;
                }
            }

            bakeCamera.gameObject.SetActive(false);
        }

        private Sprite RenderToSprite(Vector2 center, Vector2 size, int pixelWidth, int pixelHeight, float pixelsPerUnit)
        {
            // The Tilemaps render at z=0; the camera must sit strictly outside its own near clip
            // plane looking at them (matches Main Camera's -10 convention), or it captures
            // nothing at all.
            var cameraTransform = bakeCamera.transform;
            cameraTransform.position = new Vector3(center.x, center.y, -cameraDistance);
            bakeCamera.orthographicSize = size.y * 0.5f;

            var renderTexture = new RenderTexture(pixelWidth, pixelHeight, 0, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };

            bakeCamera.targetTexture = renderTexture;
            bakeCamera.Render();

            var previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;

            var texture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, pixelWidth, pixelHeight), 0, 0);
            texture.Apply();

            RenderTexture.active = previousActive;
            bakeCamera.targetTexture = null;
            renderTexture.Release();

            return Sprite.Create(texture, new Rect(0, 0, pixelWidth, pixelHeight), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }
    }
}
