using UnityEngine;
using EscapeThe90s.Contracts;

[RequireComponent(
    typeof(PlayerControllerTL6),
    typeof(PlayerCombatController),
    typeof(PlayerHealth)
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

    public void SetPermanentHealthBonus(float totalBonus)
    {
        GetComponent<PlayerHealth>()
            .SetPermanentHealthBonus(Mathf.RoundToInt(totalBonus));
    }
}