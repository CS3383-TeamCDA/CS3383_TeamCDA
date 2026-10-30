#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class GroundEnemyPursuitTests
{
    private GameObject root;
    private GroundEnemy enemy;
    private Transform player;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        root = new GameObject("Pursuit test");
        var floor = new GameObject("Ground");
        floor.transform.SetParent(root.transform);
        floor.layer = LayerMask.NameToLayer("Ground");
        floor.transform.position = new Vector3(0, -.5f, 0);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(40, 1);
        player = new GameObject("Player target").transform;
        player.SetParent(root.transform);
        player.position = new Vector3(8, 0, 0);
        var prefab = AssetDatabase.LoadAssetAtPath<GroundEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab");
        enemy = Object.Instantiate(prefab, new Vector3(0, .05f, 0), Quaternion.identity, root.transform);
        // Movement checks need the target inside either starting radius, regardless of prefab defaults.
        enemy.SetAwareness(EnemyAwareness.High);
        enemy.SetTarget(player);
        enemy.GetComponent<EnemyAttackAnimations>().enabled = false;
        yield return new WaitForFixedUpdate();
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PursuesTurnsStopsNearbyAndStopsOnDeath()
    {
        yield return new WaitForSeconds(.7f);
        Assert.Greater(enemy.transform.position.x, .6f);
        Assert.IsTrue(enemy.FacingRight);
        Assert.IsTrue(enemy.GetComponent<Animator>().GetBool("Moving"));
        player.position = new Vector3(-8, 0, 0);
        float before = enemy.transform.position.x;
        yield return new WaitForSeconds(.7f);
        Assert.Less(enemy.transform.position.x, before - .5f);
        Assert.IsFalse(enemy.FacingRight);
        player.position = enemy.transform.position;
        yield return new WaitForSeconds(.3f);
        Assert.Less(Mathf.Abs(enemy.GetComponent<Rigidbody2D>().linearVelocity.x), .01f);
        Assert.IsFalse(enemy.GetComponent<Animator>().GetBool("Moving"));
        player.position = new Vector3(8, 0, 0);
        yield return new WaitForSeconds(.3f);
        enemy.TakeDamage(enemy.MaxHealth);
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(0, enemy.GetComponent<Rigidbody2D>().linearVelocity.x);
    }

    [UnityTest]
    public IEnumerator AwarenessAcquiresWithinRangeAndKeepsPursuitUntilDeath()
    {
        foreach (var range in new[] { EnemyAwareness.Low, EnemyAwareness.High })
        {
            Object.Destroy(enemy.gameObject);
            yield return null;
            var prefab = AssetDatabase.LoadAssetAtPath<GroundEnemy>(
                "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab");
            enemy = Object.Instantiate(prefab, new Vector3(0, .02f, 0), Quaternion.identity, root.transform);
            enemy.GetComponent<EnemyAttackAnimations>().enabled = false;
            enemy.SetAwareness(range);
            enemy.SetTarget(player);
            var body = enemy.GetComponent<Rigidbody2D>();
            body.position = new Vector2(0, .02f);
            body.linearVelocity = Vector2.zero;
            float radius = enemy.ActiveAggroRadius;
            player.position = new Vector3(radius + 2, 0, 0);
            yield return new WaitForSeconds(.3f);
            Assert.That(enemy.transform.position.x, Is.EqualTo(0).Within(.05f), range + " must ignore distant targets.");
            player.position = new Vector3(radius - 1, 0, 0);
            yield return new WaitForSeconds(.3f);
            Assert.Greater(enemy.transform.position.x, .2f, range + " must pursue targets inside its radius.");
            player.position = new Vector3(radius + 5, 0, 0);
            yield return new WaitForSeconds(.3f);
            Assert.IsTrue(enemy.HasAggro);
            Assert.Greater(body.linearVelocity.x, .1f, range + " must keep pursuit outside detection radius.");
            player.position = new Vector3(100, 0, 0);
            yield return new WaitForSeconds(.3f);
            Assert.Greater(body.linearVelocity.x, .1f, "Distance must never clear acquired aggro.");
            enemy.TakeDamage(enemy.MaxHealth);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(enemy.HasAggro);
            Assert.AreEqual(0, body.linearVelocity.x);
        }
    }

    [UnityTest]
    public IEnumerator CrowdOverlapsVisuallyWithoutPushingOrPassingThrough()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GroundEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab");
        var leader = Object.Instantiate(prefab, new Vector3(1.2f, .05f, 0), Quaternion.identity, root.transform);
        leader.SetTarget(null);
        yield return new WaitForSeconds(1.2f);
        float separation = leader.transform.position.x - enemy.transform.position.x;
        Assert.That(leader.transform.position.x, Is.EqualTo(1.2f).Within(.05f),
            "A pursuing enemy should not shove an idle ally.");
        Assert.That(separation, Is.InRange(.4f, .65f),
            "Enemies should partially overlap visually but retain separate bodies.");
        Assert.IsTrue(enemy.GetComponent<SpriteRenderer>().bounds.Intersects(
            leader.GetComponent<SpriteRenderer>().bounds));
        Assert.GreaterOrEqual(enemy.GetComponent<Collider2D>().Distance(
            leader.GetComponent<Collider2D>()).distance, -.02f);
    }

    [UnityTest]
    public IEnumerator JumpsOverTerrainStepWithoutFallingThroughGround()
    {
        var obstacle = new GameObject("Terrain step");
        obstacle.transform.SetParent(root.transform);
        obstacle.layer = LayerMask.NameToLayer("Ground");
        obstacle.transform.position = new Vector3(1.8f, .6f, 0);
        obstacle.AddComponent<BoxCollider2D>().size = new Vector2(1, 1.2f);
        float highest = 0;
        for (int i = 0; i < 110; i++)
        {
            yield return new WaitForFixedUpdate();
            highest = Mathf.Max(highest, enemy.transform.position.y);
            Assert.Greater(enemy.transform.position.y, -.1f);
        }
        Assert.Greater(highest, 1.2f, "Blocked ground pursuit should trigger a jump.");
        Assert.Greater(enemy.transform.position.x, 2.8f, "Enemy should clear the terrain step.");
    }
}
#endif
