using System.Collections.Generic;
using UnityEngine;

namespace CaseGame.UI.Settings
{
    /// <summary>
    /// Plain C# helper, engine-independent aside from the <see cref="Resolution"/> struct
    /// parameter: <c>Screen.resolutions</c> commonly repeats the same width/height at several
    /// refresh rates, which would show duplicate rows in a resolution selector. This collapses
    /// that down to one option per distinct width/height, ascending by width then height.
    /// </summary>
    public static class ResolutionOptionsBuilder
    {
        public static List<ResolutionOption> BuildDistinctOptions(IReadOnlyList<Resolution> resolutions)
        {
            var seen = new HashSet<(int width, int height)>();
            var options = new List<ResolutionOption>();

            foreach (var resolution in resolutions)
            {
                var key = (resolution.width, resolution.height);
                if (seen.Add(key))
                {
                    options.Add(new ResolutionOption(resolution.width, resolution.height));
                }
            }

            options.Sort((a, b) =>
            {
                var widthCompare = a.Width.CompareTo(b.Width);
                return widthCompare != 0 ? widthCompare : a.Height.CompareTo(b.Height);
            });

            return options;
        }

        /// <summary>Index of the exact width/height match, or the last option if none matches, or -1 if there are no options.</summary>
        public static int FindClosestIndex(IReadOnlyList<ResolutionOption> options, int currentWidth, int currentHeight)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].Width == currentWidth && options[i].Height == currentHeight)
                {
                    return i;
                }
            }

            return options.Count > 0 ? options.Count - 1 : -1;
        }
    }
}
