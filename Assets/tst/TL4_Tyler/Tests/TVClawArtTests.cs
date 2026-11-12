#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class TVClawArtTests
{
    private const string Prefab = "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/TVClaw.prefab";
    [Test] public void PrefabUsesAllSevenAtlasSequencesWithoutLegacyAnimator()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<TVClawEnemy>(Prefab);
        var driver = prefab.GetComponent<TVClawArtDriver>();
        Assert.IsNotNull(driver);
        Assert.IsFalse(prefab.GetComponent<Animator>().enabled);
        var art = driver.Art;
        Assert.IsNotNull(art);
        foreach (var frames in new[] { art.idle, art.run, art.swipe, art.bite, art.pounce, art.hit, art.death })
        {
            Assert.AreEqual(8, frames.Length);
            foreach (var frame in frames) Assert.IsNotNull(frame);
        }
        Assert.AreEqual(Color.white, prefab.GetComponent<SpriteRenderer>().color);
    }
    [UnityTest] public IEnumerator HunterRunsThroughLivingEnemiesCorpsesAndNewArrivals()
    {
        var claw = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TVClawEnemy>(Prefab));
        var ally = new GameObject("Living enemy");
        var player = new GameObject("Target player");
        GameObject corpse = null;
        try
        {
            var other = ally.AddComponent<GroundEnemy>();
            var otherCollider = ally.AddComponent<BoxCollider2D>();
            ally.transform.position = Vector2.right * .5f;
            player.transform.position = Vector2.right * 8;
            var playerCollider = player.AddComponent<BoxCollider2D>();
            player.AddComponent<PlayerHealth>();
            claw.SetTarget(player.transform);
            Physics2D.SyncTransforms();
            claw.Move();
            Assert.IsTrue(Physics2D.GetIgnoreCollision(claw.GetComponent<Collider2D>(), otherCollider));
            Assert.Greater(claw.GetComponent<Rigidbody2D>().linearVelocity.x, 0, "No crowd-following clamp.");
            other.TakeDamage(other.CurrentHealth);
            claw.Move();
            Assert.IsTrue(Physics2D.GetIgnoreCollision(claw.GetComponent<Collider2D>(), otherCollider));
            corpse = new GameObject("New corpse");
            var dead = corpse.AddComponent<GroundEnemy>();
            var deadCollider = corpse.AddComponent<BoxCollider2D>();
            dead.TakeDamage(dead.CurrentHealth);
            claw.Move();
            Assert.IsTrue(Physics2D.GetIgnoreCollision(claw.GetComponent<Collider2D>(), deadCollider));
            Assert.IsFalse(Physics2D.GetIgnoreCollision(claw.GetComponent<Collider2D>(), playerCollider));
        }
        finally
        {
            Object.Destroy(claw.gameObject); Object.Destroy(ally); Object.Destroy(player);
            if (corpse != null) Object.Destroy(corpse);
        }
        yield return null;
    }

    [UnityTest] public IEnumerator BiteWindupRecoveryAndDeathUseTheirOwnFrames()
    {
        var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TVClawEnemy>(Prefab));
        var target = new GameObject("Art test player");
        target.transform.position = Vector2.right * .8f;
        target.AddComponent<PlayerHealth>();
        target.AddComponent<BoxCollider2D>();
        try
        {
            enemy.GetComponent<Rigidbody2D>().simulated = false;
            var driver = enemy.GetComponent<TVClawArtDriver>();
            var renderer = enemy.GetComponent<SpriteRenderer>();
            enemy.TryAcquireTarget(target.transform);
            enemy.TickCombat(.01f);
            driver.Render(0);
            Assert.AreEqual(driver.Art.bite[0], renderer.sprite);
            enemy.TickCombat(enemy.BiteWindup);
            driver.Render(0);
            Assert.AreEqual(driver.Art.bite[4], renderer.sprite, "Damage frame starts recovery playback.");
            enemy.TakeDamage(enemy.CurrentHealth);
            driver.Render(0);
            Assert.AreEqual(driver.Art.death[0], renderer.sprite);
            driver.Render(1);
            Assert.AreEqual(driver.Art.death[7], renderer.sprite);
            driver.Render(1);
            Assert.AreEqual(driver.Art.death[7], renderer.sprite, "Corpse must hold the final pose.");
        }
        finally { Object.Destroy(enemy.gameObject); Object.Destroy(target); }
        yield return null;
    }
}
#endif
