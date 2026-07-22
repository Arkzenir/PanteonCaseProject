using CaseGame.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.Info
{
    /// <summary>
    /// View: one small, non-interactive icon representing a unit a selected building can
    /// produce (BRIEF.md requirement 5). Purely informational — producing a unit still only
    /// happens through the Production Menu, so unlike <c>ProductionMenuItemView</c> this has no
    /// button/click behavior.
    /// </summary>
    public class ProducibleUnitIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;

        public void Bind(UnitDefinition definition)
        {
            iconImage.sprite = definition.Sprite;

            if (nameText != null)
            {
                nameText.text = definition.EntityName;
            }
        }
    }
}
