using System.Collections.Generic;
using UnityEngine;

namespace CaseGame.UI.Production
{
    /// <summary>
    /// The infinite-scroll recycling math: given how many data items exist and how far the view
    /// has scrolled, decides which data index each of a small, fixed number of pooled slots
    /// should currently show, and where. The pool never grows with the item count — that's what
    /// makes the list capable of scrolling through arbitrarily many items. Plain C#, no Unity
    /// UI/MonoBehaviour dependency, so it's directly testable independent of ScrollRect.
    /// </summary>
    public static class ScrollRecycler
    {
        public struct SlotAssignment
        {
            public int SlotIndex;
            public int ItemIndex;
            public float AnchoredY;
        }

        /// <param name="scrollOffset">Distance scrolled down from the top, in UI units, always &gt;= 0.</param>
        public static IEnumerable<SlotAssignment> ComputeSlots(int itemCount, int slotCount, float itemHeight, float scrollOffset)
        {
            if (itemCount <= 0 || slotCount <= 0 || itemHeight <= 0f)
            {
                yield break;
            }

            var firstVisibleIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, scrollOffset) / itemHeight), 0, itemCount - 1);

            for (var slot = 0; slot < slotCount; slot++)
            {
                var itemIndex = firstVisibleIndex + slot;
                if (itemIndex >= itemCount)
                {
                    yield break;
                }

                yield return new SlotAssignment
                {
                    SlotIndex = slot,
                    ItemIndex = itemIndex,
                    AnchoredY = -itemIndex * itemHeight,
                };
            }
        }

        public static float ComputeContentHeight(int itemCount, float itemHeight)
        {
            return Mathf.Max(0, itemCount) * itemHeight;
        }
    }
}
