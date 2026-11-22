using UnityEngine;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.MetaProgression
{
    public class AttackEffect : UpgradeEffect
    {

        public override void Apply(float value, IPlayerPermanentUpgradeReceiver playerUpgrade)
        {
            if(value > 0)
                playerUpgrade.SetPermanentAttackBonus(value);
        }
    }
}
