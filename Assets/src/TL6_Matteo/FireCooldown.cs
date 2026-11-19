using UnityEngine;

public sealed class FireCooldown
{
    private readonly float _secondsBetweenShots;
    private float _nextFireTime;

    public FireCooldown(float secondsBetweenShots)
    {
        _secondsBetweenShots =
            Mathf.Max(0f, secondsBetweenShots);
    }

    public bool CanFire(bool fireHeld, float currentTime)
    {
        return fireHeld && currentTime >= _nextFireTime;
    }

    public void RecordSuccessfulShot(float currentTime)
    {
        _nextFireTime = currentTime + _secondsBetweenShots;
    }
}
