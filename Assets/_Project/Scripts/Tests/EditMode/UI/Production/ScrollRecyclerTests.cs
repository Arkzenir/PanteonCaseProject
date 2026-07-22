using System.Collections.Generic;
using System.Linq;
using CaseGame.UI.Production;
using NUnit.Framework;

namespace CaseGame.Tests.EditMode.UI.Production
{
    public class ScrollRecyclerTests
    {
        [Test]
        public void ComputeSlots_ZeroOffset_AssignsSlotsToFirstItemsInOrder()
        {
            var slots = ScrollRecycler.ComputeSlots(itemCount: 10, slotCount: 4, itemHeight: 100f, scrollOffset: 0f).ToList();

            Assert.AreEqual(4, slots.Count);
            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(i, slots[i].SlotIndex);
                Assert.AreEqual(i, slots[i].ItemIndex);
                Assert.AreEqual(-i * 100f, slots[i].AnchoredY);
            }
        }

        [Test]
        public void ComputeSlots_ScrolledOffset_ShiftsAssignedItemIndices()
        {
            var slots = ScrollRecycler.ComputeSlots(itemCount: 10, slotCount: 3, itemHeight: 100f, scrollOffset: 250f).ToList();

            // 250 / 100 = 2.5 -> first visible item is index 2
            Assert.AreEqual(new[] { 2, 3, 4 }, slots.Select(s => s.ItemIndex).ToArray());
        }

        [Test]
        public void ComputeSlots_FewerItemsThanSlots_OnlyAssignsAvailableItems()
        {
            var slots = ScrollRecycler.ComputeSlots(itemCount: 2, slotCount: 5, itemHeight: 100f, scrollOffset: 0f).ToList();

            Assert.AreEqual(2, slots.Count);
            Assert.AreEqual(new[] { 0, 1 }, slots.Select(s => s.ItemIndex).ToArray());
        }

        [Test]
        public void ComputeSlots_ScrolledPastEnd_ClampsToLastReachableWindow()
        {
            var slots = ScrollRecycler.ComputeSlots(itemCount: 5, slotCount: 3, itemHeight: 100f, scrollOffset: 10000f).ToList();

            // Clamped so the window never runs off the end of the list.
            Assert.AreEqual(new[] { 4 }, slots.Select(s => s.ItemIndex).ToArray());
        }

        [Test]
        public void ComputeSlots_ZeroItemCount_YieldsNothing()
        {
            var slots = ScrollRecycler.ComputeSlots(itemCount: 0, slotCount: 4, itemHeight: 100f, scrollOffset: 0f).ToList();

            CollectionAssert.IsEmpty(slots);
        }

        [Test]
        public void ComputeSlots_ZeroSlotCount_YieldsNothing()
        {
            var slots = ScrollRecycler.ComputeSlots(itemCount: 10, slotCount: 0, itemHeight: 100f, scrollOffset: 0f).ToList();

            CollectionAssert.IsEmpty(slots);
        }

        [Test]
        public void ComputeContentHeight_ReturnsItemCountTimesItemHeight()
        {
            Assert.AreEqual(500f, ScrollRecycler.ComputeContentHeight(5, 100f));
        }

        [Test]
        public void ComputeContentHeight_ZeroItems_ReturnsZero()
        {
            Assert.AreEqual(0f, ScrollRecycler.ComputeContentHeight(0, 100f));
        }
    }
}
