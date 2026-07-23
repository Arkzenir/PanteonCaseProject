using CaseGame.Environment;
using NUnit.Framework;

namespace CaseGame.Tests.EditMode.Environment
{
    public class TerrainBakeResolutionTests
    {
        [Test]
        public void Compute_UnderCeiling_KeepsNativePixelsPerUnit()
        {
            var (pixelWidth, pixelHeight, pixelsPerUnit) =
                TerrainBakeResolution.Compute(worldWidth: 10f, worldHeight: 8f, nativePixelsPerUnit: 64f, maxDimension: 4096);

            Assert.AreEqual(64f, pixelsPerUnit);
            Assert.AreEqual(640, pixelWidth);
            Assert.AreEqual(512, pixelHeight);
        }

        [Test]
        public void Compute_OverCeiling_ScalesDownUniformlyPreservingAspect()
        {
            var (pixelWidth, pixelHeight, pixelsPerUnit) =
                TerrainBakeResolution.Compute(worldWidth: 80f, worldHeight: 72f, nativePixelsPerUnit: 64f, maxDimension: 4096);

            // Native would be 5120x4608 - width is the binding dimension.
            Assert.AreEqual(4096, pixelWidth);
            Assert.Less(pixelsPerUnit, 64f);
            Assert.AreEqual(pixelWidth / 80f, pixelsPerUnit, 0.001f);
            Assert.AreEqual(72f * pixelsPerUnit, pixelHeight, 1f);
        }

        [Test]
        public void Compute_TallerThanWide_HeightIsBindingDimension()
        {
            var (pixelWidth, pixelHeight, pixelsPerUnit) =
                TerrainBakeResolution.Compute(worldWidth: 40f, worldHeight: 100f, nativePixelsPerUnit: 64f, maxDimension: 2048);

            Assert.AreEqual(2048, pixelHeight);
            Assert.AreEqual(pixelHeight / 100f, pixelsPerUnit, 0.001f);
            Assert.AreEqual(40f * pixelsPerUnit, pixelWidth, 1f);
        }
    }
}
