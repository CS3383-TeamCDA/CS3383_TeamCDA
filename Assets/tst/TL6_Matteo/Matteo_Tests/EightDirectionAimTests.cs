using NUnit.Framework;
using UnityEngine;

public class EightDirectionAimTests
{
    [TestCase(10f, 0f)]
    [TestCase(30f, 45f)]
    [TestCase(80f, 90f)]
    [TestCase(130f, 135f)]
    [TestCase(170f, 180f)]
    [TestCase(190f, 180f)]
    [TestCase(230f, 225f)]
    [TestCase(260f, 270f)]
    [TestCase(310f, 315f)]
    public void AimSnapsToNearest45Degrees(
        float inputAngle,
        float expectedAngle)
    {
        Vector2 input = DirectionFromAngle(inputAngle);
        Vector2 expected = DirectionFromAngle(expectedAngle);

        Vector2 result = EightDirectionAim.Snap(input);

        Assert.Less(
            Vector2.Distance(result, expected),
            0.001f
        );
    }

    [Test]
    public void ZeroDirectionReturnsZero()
    {
        Vector2 result = EightDirectionAim.Snap(Vector2.zero);

        Assert.AreEqual(Vector2.zero, result);
    }

    [Test]
    public void AimOutputIsNormalized()
    {
        Vector2 direction = new Vector2(5f, 3f);

        Vector2 result = EightDirectionAim.Snap(direction);

        Assert.AreEqual(1f, result.magnitude, 0.001f);
    }

    [Test]
    public void AimSnappingDoesNotDependOnDistance()
    {
        Vector2 near = new Vector2(1f, 1f);
        Vector2 far = new Vector2(100f, 100f);

        Vector2 nearResult = EightDirectionAim.Snap(near);
        Vector2 farResult = EightDirectionAim.Snap(far);

        Assert.Less(
            Vector2.Distance(nearResult, farResult),
            0.001f
        );
    }

    [Test]
    public void AimSnapsCorrectlyInNegativeAngleRange()
    {
        Vector2 direction = DirectionFromAngle(-80f);
        Vector2 expected = DirectionFromAngle(-90f);

        Vector2 result = EightDirectionAim.Snap(direction);

        Assert.Less(
            Vector2.Distance(result, expected),
            0.001f
        );
    }

    private Vector2 DirectionFromAngle(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );
    }
}
