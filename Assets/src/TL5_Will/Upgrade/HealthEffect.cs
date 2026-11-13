using UnityEngine;

namespace EscapeThe90s.MetaProgression
{
    public class HealthEffect : UpgradeEffect
    {
        private PlayerHealth health;
        public override void Apply(float value)
        {
            if(value > 0)
                health.IncreaseMaxHealth((int)value);
        }
    }
}
