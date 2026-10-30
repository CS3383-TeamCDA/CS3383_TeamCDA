#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyAttackAnimationTests
{
    private GroundEnemy enemy;
    private GameObject target;
    private EnemyAttackAnimations attacks;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GroundEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab");
        enemy = Object.Instantiate(prefab);
        enemy.GetComponent<Rigidbody2D>().simulated = false;
        enemy.SetAwareness(EnemyAwareness.High);
        target = new GameObject("Attack target");
        target.transform.position = Vector3.right * 5;
        attacks = enemy.GetComponent<EnemyAttackAnimations>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>())
            if (projectile.Owner == enemy) Object.Destroy(projectile.gameObject);
        Object.Destroy(enemy.gameObject);
        Object.Destroy(target);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RangedAndCloseTriggersPlayTheirClipsWithoutSpamming()
    {
        SetSecondShotChance(1); // Closing into melee must cancel even a guaranteed follow-up.
        SetMeleeChance(0);
        int meleeCount = 0, rangedCount = 0;
        attacks.AttackTriggered += kind =>
        {
            if (kind == EnemyAttackAnimation.Melee) meleeCount++;
            if (kind == EnemyAttackAnimation.Ranged) rangedCount++;
        };
        Assert.IsTrue(enemy.TryAcquireTarget(target.transform));
        Assert.AreEqual(EnemyAttackAnimation.Ranged, attacks.TryTriggerAttack());
        var animator = enemy.GetComponent<Animator>();
        animator.Update(.01f);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Shoot"));
        target.transform.position = Vector3.left;
        Assert.AreEqual(EnemyAttackAnimation.None, attacks.TryTriggerAttack(), "Melee must respect the shared cooldown.");
        float firstMeleeDeadline = Time.time + 1f;
        while (meleeCount == 0 && Time.time < firstMeleeDeadline) yield return null;
        yield return null;
        Assert.AreEqual(1, rangedCount);
        Assert.AreEqual(1, meleeCount);
        Assert.IsFalse(enemy.FacingRight);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Melee"));
    }

    private void SetSecondShotChance(float chance)
    {
        typeof(EnemyAttackAnimations).GetField("secondShotChance", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(attacks, chance);
    }

    [UnityTest]
    public IEnumerator TwoShotSequenceLocksHorizontalMovementAndThenReleasesPursuit()
    {
        SetSecondShotChance(1);
        int shots = 0;
        float firstShotTime = 0, secondShotTime = 0;
        enemy.GetComponent<EnemyRangedAttack>().ShotFired += _ =>
        {
            shots++;
            if (shots == 1) firstShotTime = Time.time;
            if (shots == 2) secondShotTime = Time.time;
        };
        var body = enemy.GetComponent<Rigidbody2D>();
        body.simulated = true;
        body.gravityScale = 0;
        body.linearVelocity = new Vector2(3, -.2f);
        Assert.IsTrue(enemy.TryAcquireTarget(target.transform));
        Assert.AreEqual(EnemyAttackAnimation.Ranged, attacks.TryTriggerAttack());
        Assert.AreEqual(0, body.linearVelocity.x);
        float x = enemy.transform.position.x;
        bool sawPause = false, sawRecovery = false;
        float deadline = Time.time + 5;
        while (attacks.IsMovementLocked && Time.time < deadline)
        {
            sawPause |= attacks.Phase == EnemyAttackPhase.FollowUpPause;
            sawRecovery |= attacks.Phase == EnemyAttackPhase.Recovering;
            Assert.That(enemy.transform.position.x, Is.EqualTo(x).Within(.03f));
            yield return null;
        }
        Assert.AreEqual(2, shots, "A committed burst must produce exactly two projectiles.");
        Assert.IsTrue(sawPause && sawRecovery);
        Assert.Greater(secondShotTime - firstShotTime, .8f);
        Assert.Less(enemy.transform.position.y, -.1f, "The attack lock must preserve vertical motion.");
        Assert.IsFalse(attacks.IsMovementLocked);
        yield return new WaitForSeconds(.25f);
        Assert.Greater(enemy.transform.position.x, x + .1f, "Pursuit must resume between sequences.");
        Assert.IsTrue(enemy.HasAggro);
    }

    private void SetMeleeChance(float chance)
    {
        typeof(EnemyAttackAnimations).GetField("secondMeleeChance", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(attacks, chance);
    }

    [UnityTest]
    public IEnumerator DoubleMeleeHasExactlyTwoSwingsWithPauseAndMovementLock()
    {
        SetMeleeChance(1);
        target.transform.position = Vector3.right;
        Assert.IsTrue(enemy.TryAcquireTarget(target.transform));
        int swings = 0;
        float firstTime = 0, secondTime = 0;
        attacks.AttackTriggered += kind =>
        {
            if (kind != EnemyAttackAnimation.Melee) return;
            swings++;
            if (swings == 1) firstTime = Time.time;
            if (swings == 2) secondTime = Time.time;
        };
        Assert.AreEqual(EnemyAttackAnimation.Melee, attacks.TryTriggerAttack());
        bool paused = false;
        float deadline = Time.time + 4;
        while (attacks.IsMovementLocked && Time.time < deadline)
        {
            paused |= attacks.Phase == EnemyAttackPhase.FollowUpPause;
            yield return null;
        }
        Assert.AreEqual(2, swings);
        Assert.IsTrue(paused);
        Assert.GreaterOrEqual(secondTime - firstTime,
            enemy.GetComponent<EnemyAnimationDriver>().AttackDuration(EnemyAttackAnimation.Melee) + .14f);
    }

    [UnityTest]
    public IEnumerator LeavingMeleeRangeCancelsTheSecondSwing()
    {
        SetMeleeChance(1);
        target.transform.position = Vector3.right;
        Assert.IsTrue(enemy.TryAcquireTarget(target.transform));
        int swings = 0;
        attacks.AttackTriggered += kind => { if (kind == EnemyAttackAnimation.Melee) swings++; };
        attacks.TryTriggerAttack();
        target.transform.position = Vector3.right * 20;
        yield return new WaitForSeconds(2);
        Assert.AreEqual(1, swings);
        Assert.IsTrue(enemy.HasAggro);
    }

    [UnityTest]
    public IEnumerator DeathCancelsPendingFollowUpProjectile()
    {
        SetSecondShotChance(1);
        int shots = 0;
        enemy.GetComponent<EnemyRangedAttack>().ShotFired += _ => shots++;
        Assert.IsTrue(enemy.TryAcquireTarget(target.transform));
        attacks.TryTriggerAttack();
        Assert.AreEqual(1, shots);
        enemy.TakeDamage(enemy.MaxHealth);
        yield return new WaitForSeconds(2);
        Assert.AreEqual(1, shots);
        Assert.IsFalse(attacks.IsMovementLocked);
    }

    [UnityTest]
    public IEnumerator RequiresAggroRangeAllowedAttackAndLivingEnemy()
    {
        Assert.AreEqual(EnemyAttackAnimation.None, attacks.TryTriggerAttack());
        Assert.IsTrue(enemy.TryAcquireTarget(target.transform));
        target.transform.position = Vector3.right * 20;
        Assert.AreEqual(EnemyAttackAnimation.None, attacks.TryTriggerAttack(), "Persistent aggro does not grant unlimited attack range.");
        target.transform.position = Vector3.right * 5;
        enemy.SetAttackMode(EnemyAttackMode.MeleeOnly);
        Assert.AreEqual(EnemyAttackAnimation.None, attacks.TryTriggerAttack());
        target.transform.position = Vector3.right;
        Assert.AreEqual(EnemyAttackAnimation.Melee, attacks.TryTriggerAttack());
        enemy.TakeDamage(enemy.MaxHealth);
        yield return new WaitForSeconds(1.4f);
        Assert.AreEqual(EnemyAttackAnimation.None, attacks.TryTriggerAttack());
        Assert.IsFalse(enemy.GetComponent<EnemyAnimationDriver>().TriggerMelee());
        Assert.IsFalse(enemy.GetComponent<EnemyAnimationDriver>().TriggerRanged());
    }
}
#endif
