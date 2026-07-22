using System.Collections.Generic;
using CaseGame.Buildings;
using CaseGame.Pooling;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.Production
{
    /// <summary>
    /// Controller: the Production Menu's infinite, pooled scroll view (UX brief: "Infinite
    /// Scrollview — Object Pooling"). Pools a small, fixed number of <see cref="ProductionMenuItemView"/>
    /// rows via <see cref="PrefabPool{T}"/> — not one row per <see cref="BuildingCatalog"/> entry
    /// — and rebinds them to different entries as the view scrolls, per <see cref="ScrollRecycler"/>'s
    /// index math. Iterates the catalog generically; no per-building-type branch exists here
    /// (BRIEF.md requirement 2).
    /// </summary>
    public class ProductionMenuController : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private ProductionMenuItemView itemPrefab;
        [SerializeField] private BuildingCatalog catalog;
        [SerializeField] private float itemHeight = 96f;
        [SerializeField] private int poolSize = 8;

        private PrefabPool<ProductionMenuItemView> _pool;
        private readonly List<ProductionMenuItemView> _activeSlots = new List<ProductionMenuItemView>();
        private readonly List<int> _slotItemIndex = new List<int>();

        /// <summary>The pooled row views currently in use, in slot order. Read-only — exposed for observers/tests to inspect what each slot is bound to; the count never exceeds <c>poolSize</c> regardless of catalog size.</summary>
        public IReadOnlyList<ProductionMenuItemView> ActiveSlots => _activeSlots;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            scrollRect.onValueChanged.AddListener(OnScrollChanged);
            RefreshLayout();
        }

        private void OnDisable()
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        /// <summary>Builds the pool and its fixed set of slots. Called from <see cref="Awake"/> at runtime; exposed publicly (idempotent) so tests can call it directly instead of depending on Awake actually firing — see <c>ENVIRONMENT.md</c>'s note that Awake doesn't reliably run on AddComponent-created objects under this machine's batchmode EditMode test runner.</summary>
        public void Initialize()
        {
            if (_pool != null)
            {
                return;
            }

            _pool = new PrefabPool<ProductionMenuItemView>(itemPrefab, content);
            BuildSlots();
        }

        /// <summary>Rebuilds slot bindings for the current catalog and scroll position. Public — callable directly, independent of the ScrollRect callback, for testability and for re-running after the catalog changes.</summary>
        public void RefreshLayout()
        {
            var itemCount = catalog != null ? catalog.Entries.Count : 0;
            content.sizeDelta = new Vector2(content.sizeDelta.x, ScrollRecycler.ComputeContentHeight(itemCount, itemHeight));
            ApplyRecycling(0f);
        }

        /// <summary>Rebinds/repositions pooled slots for the given scroll offset. Public and independent of <see cref="ScrollRect"/>'s callback so tests can drive it directly, mirroring the "extract the testable decision, keep the callback thin" pattern used by <c>PlacementController</c>.</summary>
        public void ApplyScrollOffset(float scrollOffset)
        {
            ApplyRecycling(scrollOffset);
        }

        private void BuildSlots()
        {
            var itemCount = catalog != null ? catalog.Entries.Count : 0;
            var slotCount = Mathf.Min(poolSize, itemCount);

            for (var i = 0; i < slotCount; i++)
            {
                var view = _pool.Get();
                _activeSlots.Add(view);
                _slotItemIndex.Add(-1);
            }
        }

        private void OnScrollChanged(Vector2 normalizedPosition)
        {
            ApplyRecycling(Mathf.Abs(content.anchoredPosition.y));
        }

        private void ApplyRecycling(float scrollOffset)
        {
            if (catalog == null)
            {
                return;
            }

            foreach (var assignment in ScrollRecycler.ComputeSlots(catalog.Entries.Count, _activeSlots.Count, itemHeight, scrollOffset))
            {
                if (_slotItemIndex[assignment.SlotIndex] == assignment.ItemIndex)
                {
                    continue;
                }

                var view = _activeSlots[assignment.SlotIndex];
                view.Bind(catalog.Entries[assignment.ItemIndex]);

                var rect = (RectTransform)view.transform;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, assignment.AnchoredY);

                _slotItemIndex[assignment.SlotIndex] = assignment.ItemIndex;
            }
        }
    }
}
