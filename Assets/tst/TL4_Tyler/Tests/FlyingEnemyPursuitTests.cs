#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class FlyingEnemyPursuitTests
{
    private GameObject root;
    private FlyingEnemy enemy;
    private Transform target;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        root = new GameObject("Flight test");
        target = new GameObject("Player").transform;
        target.SetParent(root.transform);
        target.position = new Vector3(5, 0, 0);
        var prefab = AssetDatabase.LoadAssetAtPath<FlyingEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Flying/Level1_Flying.prefab");
        enemy = Object.Instantiate(prefab, new Vector3(0, 3, 0), Quaternion.identity, root.transform);
        enemy.SetTarget(target);
        enemy.GetComponent<EnemyAttackAnimations>().enabled = false;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FliesTowardPlayerTurnsAndKeepsAggroUntilDeath()
    {
        Assert.IsTrue(enemy.TryAcquireTarget(target));
        target.position = new Vector3(10, 0, 0);
        yield return new WaitForSeconds(.7f);
        Assert.IsTrue(enemy.HasAggro);
        Assert.Greater(enemy.transform.position.x, .8f);
        Assert.IsTrue(enemy.GetComponent<Animator>().GetBool("Moving"));
        Assert.IsFalse(enemy.GetComponent<SpriteRenderer>().flipX);
        target.position = new Vector3(-40, 0, 0);
        yield return new WaitForSeconds(.8f);
        Assert.Less(enemy.GetComponent<Rigidbody2D>().linearVelocity.x, -.5f);
        Assert.IsTrue(enemy.GetComponent<SpriteRenderer>().flipX);
        Assert.IsTrue(enemy.HasAggro);
        enemy.TakeDamage(enemy.MaxHealth);
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(Vector2.zero, enemy.GetComponent<Rigidbody2D>().linearVelocity);
    }

    [UnityTest]
    public IEnumerator ClimbsAroundTerrainWithoutPassingThroughIt()
    {
        Assert.IsTrue(enemy.TryAcquireTarget(target));
        target.position = new Vector3(10, 0, 0);
        var wall = new GameObject("Terrain rise");
        wall.transform.SetParent(root.transform);
        wall.layer = LayerMask.NameToLayer("Ground");
        wall.transform.position = new Vector3(2.5f, 1.5f, 0);
        var obstacle = wall.AddComponent<BoxCollider2D>();
        obstacle.size = new Vector2(1, 3);
        float highest = enemy.transform.position.y;
        for (int i = 0; i < 180; i++)
        {
            yield return new WaitForFixedUpdate();
            highest = Mathf.Max(highest, enemy.transform.position.y);
            Assert.Greater(enemy.GetComponent<Collider2D>().Distance(obstacle).distance, -.05f);
        }
        Assert.Greater(highest, 3.3f);
        Assert.Greater(enemy.transform.position.x, 3.3f);
    }

    [UnityTest]
    public IEnumerator KeepsStandoffBoxAndFollowsPlayerHeight()
    {
        Assert.IsTrue(enemy.TryAcquireTarget(target));
        target.position = Vector3.zero;
        yield return new WaitForSeconds(3);
        Assert.That(Mathf.Abs(enemy.transform.position.x), Is.InRange(2.8f, 6.2f));
        Assert.That(enemy.transform.position.y, Is.InRange(1.3f, 4.2f));
        Assert.AreEqual(0, enemy.GetComponent<Rigidbody2D>().gravityScale);
        float initialHeight = enemy.transform.position.y;
        target.position = Vector3.up * 5;
        yield return new WaitForSeconds(.6f);
        Assert.Greater(enemy.transform.position.y, initialHeight + .4f);
    }
}
#endif
