using System.Collections.Generic;
using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.PowerUps.Tests
{
    /// <summary>
    /// Stands in for TL6's PlayerControllerTL6, which needs a Rigidbody2D, input and states.
    /// Records the multipliers PlayerPowerUpTarget sends. In its own file so Unity can add it as a component.
    /// </summary>
    public class FakePlayerController : MonoBehaviour, IPlayerControllerTL6
    {
        public readonly List<float> MultipliersSent = new List<float>();

        public float LastMultiplier => MultipliersSent.Count == 0 ? 1f : MultipliersSent[MultipliersSent.Count - 1];

        public void SetTemporarySpeedMultiplier(float multiplier)
        {
            MultipliersSent.Add(multiplier);
        }

        public void ResetTemporarySpeedMultiplier()
        {
            MultipliersSent.Add(1f);
        }
    }
}
