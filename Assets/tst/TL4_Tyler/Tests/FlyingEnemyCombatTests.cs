#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class FlyingEnemyCombatTests
{
    private GameObject root;

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FiresDoubleShotHoldsStillThenMovesToDifferentPerch()
    {
        root = new GameObject("Flying combat");
        var target = new GameObject("Player").transform;
        target.SetParent(root.transform);
        var prefab = AssetDatabase.LoadAssetAtPath<FlyingEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Flying/Level1_Flying.prefab");
        var enemy = Object.Instantiate(prefab, new Vector3(-4, 2.5f, 0), Quaternion.identity, root.transform);
        enemy.SetTarget(target);
        var attacks = enemy.GetComponent<EnemyAttackAnimations>();
        typeof(EnemyAttackAnimations).GetField("secondShotChance", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(attacks, 1f);
        var positions = new List<Vector2>();
        enemy.GetComponent<EnemyRangedAttack>().ShotFired += _ =>
        {
            Assert.IsTrue(enemy.CanAttackTarget, "Every shot must be inside the player's firing box.");
            positions.Add(enemy.transform.position);
        };
        float deadline = Time.time + 14;
        while (positions.Count < 3 && Time.time < deadline)
        {
            if (attacks.IsMovementLocked)
                Assert.Less(enemy.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude, .001f,
                    "Flight must stop on both axes during the attack sequence.");
            yield return null;
        }
        Assert.GreaterOrEqual(positions.Count, 3, "Flyer should finish a burst, reposition, and fire again.");
        Assert.Less(Vector2.Distance(positions[0], positions[1]), .05f, "Both burst shots must use the same perch.");
        Assert.Greater(Vector2.Distance(positions[1], positions[2]), .7f, "A new sequence requires actual repositioning.");
        Assert.IsTrue(enemy.HasAggro);
    }

    [UnityTest]
    public IEnumerator RetreatsAndRefusesToFireWhenPlayerGetsTooClose()
    {
        root = new GameObject("Flying retreat");
        var target = new GameObject("Player").transform;
        target.SetParent(root.transform);
        var prefab = AssetDatabase.LoadAssetAtPath<FlyingEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Flying/Level1_Flying.prefab");
        var enemy = Object.Instantiate(prefab, new Vector3(.2f, 2, 0), Quaternion.identity, root.transform);
        enemy.SetTarget(target);
        Assert.IsTrue(enemy.TryAcquireTarget(target));
        Assert.IsFalse(enemy.CanAttackTarget);
        Assert.AreEqual(EnemyAttackAnimation.None, enemy.GetComponent<EnemyAttackAnimations>().TryTriggerAttack());
        yield return new WaitForSeconds(1);
        Assert.Greater(Mathf.Abs(enemy.transform.position.x), 1.2f, "Flyer must move away from the overhead dead zone.");
    }
}
#endif
