#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class TurretGameplayTests
{
    private GameObject root;

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator HighAwarenessTurretTracksShootsAndStaysStationary()
    {
        root = new GameObject("Turret gameplay");
        var prefab = AssetDatabase.LoadAssetAtPath<TurretHazard>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Turret/Level1_Turret.prefab");
        var turret = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, root.transform);
        var target = new GameObject("Player").transform;
        target.SetParent(root.transform);
        target.position = new Vector3(5, 5, 0);
        turret.SetTarget(target);
        Assert.AreEqual(EnemyAwareness.High, turret.Awareness);
        Assert.IsTrue(turret.AggroRange == EnemyAggroRange.Medium || turret.AggroRange == EnemyAggroRange.Far);
        Assert.AreEqual(EnemyAttackMode.RangedOnly, turret.AttackMode);
        var directions = new List<Vector2>();
        turret.GetComponent<EnemyRangedAttack>().ShotFired += direction => directions.Add(direction);
        yield return new WaitForSeconds(.15f);
        Assert.IsTrue(turret.HasAggro);
        Assert.Greater(directions.Count, 0, "Aim-only turret controllers must permit real firing.");
        Assert.That(Vector2.Angle(directions[0], Vector2.one), Is.LessThan(.1f));
        Assert.AreEqual(1, turret.GetComponent<Animator>().GetInteger("AimIndex"));
        target.position = new Vector3(-5, 5, 0);
        yield return new WaitForSeconds(.1f);
        Assert.AreEqual(3, turret.GetComponent<Animator>().GetInteger("AimIndex"));
        target.position = new Vector3(-40, 5, 0);
        yield return new WaitForSeconds(3);
        Assert.IsTrue(turret.HasAggro, "Leaving detection range must not erase aggro.");
        Assert.AreEqual(Vector3.zero, turret.transform.position);
        int beforeDeath = directions.Count;
        turret.TakeDamage(turret.MaxHealth);
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(beforeDeath, directions.Count);
    }

}
#endif
