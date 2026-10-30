#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyProjectileTests
{
    private GameObject root;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        root = new GameObject("Projectile test");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Object.Destroy(root);
        yield return null;
    }

    private GroundEnemy CreateEnemy()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GroundEnemy>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Animated.prefab");
        var enemy = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, root.transform);
        enemy.GetComponent<Rigidbody2D>().simulated = false;
        enemy.GetComponent<EnemyAttackAnimations>().enabled = false;
        return enemy;
    }

    [UnityTest]
    public IEnumerator VisibleBoltsFireFromCorrectSideTravelAndStopAtTerrain()
    {
        foreach (bool right in new[] { true, false })
        {
            var enemy = CreateEnemy();
            enemy.SetFacingRight(right);
            float direction = right ? 1 : -1;
            Assert.IsTrue(enemy.GetComponent<EnemyRangedAttack>().TryShoot(Vector2.right * direction * 10, out var shot));
            Assert.AreEqual(enemy, shot.Owner);
            Assert.Greater(shot.transform.position.x * direction, .4f, "Muzzle must be outside the enemy on the firing side.");
            var renderer = shot.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer.sprite);
            Assert.IsTrue(renderer.enabled);
            Assert.Greater(renderer.sortingOrder, enemy.GetComponent<SpriteRenderer>().sortingOrder);
            float start = shot.transform.position.x;
            yield return new WaitForSeconds(.1f);
            Assert.Greater((shot.transform.position.x - start) * direction, .4f);

            var wall = new GameObject("Blocking terrain");
            wall.transform.SetParent(root.transform);
            wall.layer = LayerMask.NameToLayer("Ground");
            wall.transform.position = shot.transform.position + Vector3.right * direction;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 4);
            yield return new WaitForSeconds(.3f);
            Assert.IsTrue(shot == null, "Projectile must stop at a thin terrain wall.");
            Object.Destroy(wall);
            Object.Destroy(enemy.gameObject);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator BoltsExpireWithoutHittingAnything()
    {
        var enemy = CreateEnemy();
        var prefab = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(
            "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/Level1_Ground_Projectile.prefab");
        var shot = Object.Instantiate(prefab, root.transform);
        shot.Launch(enemy, Vector2.right, 0, .1f);
        yield return new WaitForSeconds(.2f);
        Assert.IsTrue(shot == null);
    }
}
#endif
