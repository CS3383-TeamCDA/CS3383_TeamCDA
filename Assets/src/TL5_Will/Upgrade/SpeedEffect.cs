using UnityEngine;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.MetaProgression
{
    public class SpeedEffect : UpgradeEffect
    {
        private IPlayerPermanentUpgradeReceiver playerUpgrade;
        public override void Apply(float value, IPlayerPermanentUpgradeReceiver playerUpgrade)
        {
            if(value > 0)
                playerUpgrade.SetPermanentSpeedBonus(value);
        }
    }
}