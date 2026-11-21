using UnityEngine;
using EscapeThe90s.Contracts;

[RequireComponent(
    typeof(PlayerControllerTL6),
    typeof(PlayerCombatController)
)]
public class PlayerPermanentUpgradeReceiver :
    MonoBehaviour, IPlayerPermanentUpgradeReceiver
{
    public void SetPermanentSpeedBonus(float totalBonus)
    {
        GetComponent<PlayerControllerTL6>()
            .SetPermanentSpeedBonus(totalBonus);
    }

    public void SetPermanentAttackBonus(float totalBonus)
    {
        GetComponent<PlayerCombatController>()
            .SetPermanentAttackBonus(totalBonus);
    }
}