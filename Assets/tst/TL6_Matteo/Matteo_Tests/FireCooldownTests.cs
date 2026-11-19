using NUnit.Framework;

public class FireCooldownTests
{
    private FireCooldown _cooldown;

    [SetUp]
    public void SetUp()
    {
        _cooldown = new FireCooldown(0.18f);
    }

    [Test]
    public void FirstShotCanFireImmediately()
    {
        Assert.IsTrue(_cooldown.CanFire(true, 0f));
    }

    [Test]
    public void FireDoesNotOccurWhenButtonIsReleased()
    {
        Assert.IsFalse(_cooldown.CanFire(false, 0f));
    }

    [Test]
    public void CooldownPreventsImmediateSecondShot()
    {
        _cooldown.RecordSuccessfulShot(0f);

        Assert.IsFalse(_cooldown.CanFire(true, 0.01f));
    }

    [Test]
    public void FireCannotOccurBeforeCooldownExpires()
    {
        _cooldown.RecordSuccessfulShot(0f);

        Assert.IsFalse(_cooldown.CanFire(true, 0.17f));
    }

    [Test]
    public void FireCanOccurWhenCooldownExpires()
    {
        _cooldown.RecordSuccessfulShot(0f);

        Assert.IsTrue(_cooldown.CanFire(true, 0.18f));
    }

    [Test]
    public void FireCanOccurAfterCooldownExpires()
    {
        _cooldown.RecordSuccessfulShot(0f);

        Assert.IsTrue(_cooldown.CanFire(true, 0.25f));
    }

    [Test]
    public void SecondSuccessfulShotRestartsCooldown()
    {
        _cooldown.RecordSuccessfulShot(0f);

        Assert.IsTrue(_cooldown.CanFire(true, 0.18f));

        _cooldown.RecordSuccessfulShot(0.18f);

        Assert.IsFalse(_cooldown.CanFire(true, 0.20f));
        Assert.IsTrue(_cooldown.CanFire(true, 0.36f));
    }

    [Test]
    public void ReleasingFireDoesNotResetCooldown()
    {
        _cooldown.RecordSuccessfulShot(0f);

        Assert.IsFalse(_cooldown.CanFire(false, 0.05f));
        Assert.IsFalse(_cooldown.CanFire(true, 0.10f));
        Assert.IsTrue(_cooldown.CanFire(true, 0.18f));
    }

    [Test]
    public void CooldownOnlyStartsAfterSuccessfulShot()
    {
        Assert.IsTrue(_cooldown.CanFire(true, 0f));

        // Simulate a failed shot: do not record it.
        Assert.IsTrue(_cooldown.CanFire(true, 0.05f));

        // Now record a successful shot.
        _cooldown.RecordSuccessfulShot(0.05f);

        Assert.IsFalse(_cooldown.CanFire(true, 0.10f));
    }
}
