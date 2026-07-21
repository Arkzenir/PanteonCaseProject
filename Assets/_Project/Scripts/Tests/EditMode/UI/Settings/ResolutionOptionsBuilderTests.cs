using System.Collections.Generic;
using CaseGame.UI.Settings;
using NUnit.Framework;
using UnityEngine;

namespace CaseGame.Tests.EditMode.UI.Settings
{
    public class ResolutionOptionsBuilderTests
    {
        private static Resolution Make(int width, int height, int refreshRate = 60)
        {
            return new Resolution { width = width, height = height, refreshRate = refreshRate };
        }

        [Test]
        public void BuildDistinctOptions_DedupesSameWidthHeightAtDifferentRefreshRates()
        {
            var resolutions = new List<Resolution>
            {
                Make(1920, 1080, 60),
                Make(1920, 1080, 144),
                Make(1280, 720, 60),
            };

            var options = ResolutionOptionsBuilder.BuildDistinctOptions(resolutions);

            Assert.AreEqual(2, options.Count);
        }

        [Test]
        public void BuildDistinctOptions_SortsAscendingByWidthThenHeight()
        {
            var resolutions = new List<Resolution>
            {
                Make(1920, 1080),
                Make(1280, 720),
                Make(2560, 1440),
            };

            var options = ResolutionOptionsBuilder.BuildDistinctOptions(resolutions);

            Assert.AreEqual(1280, options[0].Width);
            Assert.AreEqual(1920, options[1].Width);
            Assert.AreEqual(2560, options[2].Width);
        }

        [Test]
        public void BuildDistinctOptions_EmptyInput_ReturnsEmptyList()
        {
            var options = ResolutionOptionsBuilder.BuildDistinctOptions(new List<Resolution>());

            Assert.IsEmpty(options);
        }

        [Test]
        public void FindClosestIndex_ReturnsExactMatchIndex()
        {
            var options = ResolutionOptionsBuilder.BuildDistinctOptions(new List<Resolution>
            {
                Make(1280, 720),
                Make(1920, 1080),
            });

            var index = ResolutionOptionsBuilder.FindClosestIndex(options, 1920, 1080);

            Assert.AreEqual(1, index);
        }

        [Test]
        public void FindClosestIndex_NoExactMatch_ReturnsLastIndex()
        {
            var options = ResolutionOptionsBuilder.BuildDistinctOptions(new List<Resolution>
            {
                Make(1280, 720),
                Make(1920, 1080),
            });

            var index = ResolutionOptionsBuilder.FindClosestIndex(options, 3840, 2160);

            Assert.AreEqual(1, index);
        }

        [Test]
        public void FindClosestIndex_EmptyOptions_ReturnsNegativeOne()
        {
            var index = ResolutionOptionsBuilder.FindClosestIndex(new List<ResolutionOption>(), 1920, 1080);

            Assert.AreEqual(-1, index);
        }

        [Test]
        public void Label_FormatsAsWidthXHeight()
        {
            var option = new ResolutionOption(1920, 1080);

            Assert.AreEqual("1920 x 1080", option.Label);
        }
    }
}
