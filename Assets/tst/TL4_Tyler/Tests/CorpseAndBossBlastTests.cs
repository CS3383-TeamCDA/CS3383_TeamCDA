#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class CorpseAndBossBlastTests
{
    [UnityTest] public IEnumerator DeathRemovesAllSolidAndTriggerCollisionBeforeDefeatListeners()
    {
        var root = new GameObject("Corpse regression");
        try
        {
            var enemy = root.AddComponent<GroundEnemy>();
            var body = root.AddComponent<Rigidbody2D>();
            root.AddComponent<BoxCollider2D>();
            var child = new GameObject("Inactive trigger");
            child.transform.SetParent(root.transform);
            child.AddComponent<CircleCollider2D>().isTrigger = true;
            child.SetActive(false);
            int defeats = 0;
            enemy.Defeated += _ =>
            {
                defeats++;
                foreach (var collider in root.GetComponentsInChildren<Collider2D>(true)) Assert.IsFalse(collider.enabled);
                Assert.IsFalse(body.simulated);
            };
            enemy.TakeDamage(enemy.CurrentHealth);
            enemy.TakeDamage(1);
            Assert.AreEqual(1, defeats);
            Vector3 position = root.transform.position;
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(position, root.transform.position);
        }
        finally { Object.Destroy(root); }
        yield return null;
    }

    [UnityTest] public IEnumerator BossPayloadExplosionsAnchorVisibleBaseToGround()
    {
        var root = new GameObject("Boss explosion regression");
        Sprite square = null;
        try
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(root.transform);
            floor.layer = 3;
            floor.transform.position = Vector2.down * .5f;
            floor.AddComponent<BoxCollider2D>().size = new Vector2(30, 1);
            Physics2D.SyncTransforms();
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width,
                Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
            var field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(null, square);
            var art = AssetDatabase.LoadAssetAtPath<TurretBossVfxSet>(
                "Assets/prefabs/TL4_Tyler/TurretBoss/Art/SiegeCoreAttackVfx.asset");
            field.ConfigureVfx(art);
            foreach (var state in new[] { TurretBossAttackState.GrenadeBlast, TurretBossAttackState.MissileBlast })
            {
                field.SetState(state, 0, 10);
                foreach (var effect in root.GetComponentsInChildren<TurretBossVfxBurst>())
                {
                    var bounds = effect.GetComponent<SpriteRenderer>().bounds;
                    Assert.AreEqual(0, bounds.min.y + bounds.size.y * art.groundExplosionBottomInset, .001f);
                }
            }
        }
        finally { Object.Destroy(root); if (square != null) Object.Destroy(square); }
        yield return null;
    }
}
#endif
