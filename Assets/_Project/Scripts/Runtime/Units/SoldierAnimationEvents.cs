using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Relays Unity Animation Events up to the parent <see cref="SoldierBase"/>. Animation
    /// Events call <c>SendMessage</c> on the GameObject the firing <see cref="Animator"/> itself
    /// sits on — for soldiers that's the <c>Visuals</c> child, not the prefab root where
    /// <see cref="SoldierBase"/> actually lives — so this small forwarder is the component the
    /// Animation Event's function name actually targets.
    /// </summary>
    public class SoldierAnimationEvents : MonoBehaviour
    {
        [SerializeField] private SoldierBase soldier;

        /// <summary>Wired as an Animation Event on the Shoot clip, at the moment the bow visibly releases the arrow.</summary>
        public void OnAttackRelease()
        {
            if (soldier != null)
            {
                soldier.ReleaseAttack();
            }
        }
    }
}
