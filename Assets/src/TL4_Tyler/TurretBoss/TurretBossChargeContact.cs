using System;
using UnityEngine;

/// <summary>Contact notification only; the owning attack field enforces active state and one hit.</summary>
public sealed class TurretBossChargeContact : MonoBehaviour
{
    private PlayerHealth target;
    private Action touched;
    public void Bind(PlayerHealth player, Action onTouch) { target = player; touched = onTouch; }
    private void OnTriggerEnter2D(Collider2D other) { Check(other); }
    private void OnTriggerStay2D(Collider2D other) { Check(other); }
    private void Check(Collider2D other)
    {
        if (target != null && other.enabled && !other.isTrigger && other.GetComponentInParent<PlayerHealth>() == target) { touched?.Invoke(); }
    }
}
