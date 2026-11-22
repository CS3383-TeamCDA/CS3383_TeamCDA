using UnityEngine;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.MetaProgression
{
    public class UpgradeEffect
    {
        public virtual void Apply(float value, IPlayerPermanentUpgradeReceiver playerUpgrade){}
    }
}

