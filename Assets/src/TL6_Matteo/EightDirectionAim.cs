using UnityEngine;

public static class EightDirectionAim
{
    public static Vector2 Snap(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return Vector2.zero;

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        float snappedAngle =
            Mathf.Round(angle / 45f) * 45f;

        float radians = snappedAngle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );
    }
}
