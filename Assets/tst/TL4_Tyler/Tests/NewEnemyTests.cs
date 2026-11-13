#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using EscapeThe90s.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class NewEnemyTests
{
    private const string Folder = "Assets/prefabs/TL4_Tyler/Enemy_Sprites/Level1_Ground/";
    private GameObject root;

    [SetUp] public void Setup() { root = new GameObject("New enemy tests"); }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Object.Destroy(root);
        foreach (var grenade in Object.FindObjectsByType<BerserkerGrenade>()) Object.Destroy(grenade.gameObject);
        foreach (var flash in Object.FindObjectsByType<EnemyBlastFlash>()) Object.Destroy(flash.gameObject);
        foreach (var burst in Object.FindObjectsByType<TurretBossVfxBurst>()) Object.Destroy(burst.gameObject);
        foreach (var air in Object.FindObjectsByType<EnemyAirBurst>()) Object.Destroy(air.gameObject);
        yield return null;
    }

    private T Enemy<T>(string name) where T : EnemyBase
    {
        var prefab = AssetDatabase.LoadAssetAtPath<T>(Folder + name + ".prefab");
        Assert.IsNotNull(prefab);
        var enemy = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, root.transform);
        enemy.GetComponent<Rigidbody2D>().simulated = false;
        return enemy;
    }
    private PlayerHealth Player(Vector2 position, bool extraCollider = false)
    {
        var go = new GameObject("Player");
        go.transform.SetParent(root.transform);
        go.transform.position = position;
        var health = go.AddComponent<PlayerHealth>();
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        if (extraCollider)
        {
            var child = new GameObject("Second hurtbox");
            child.transform.SetParent(go.transform, false);
            child.AddComponent<CircleCollider2D>().isTrigger = true;
        }
        Physics2D.SyncTransforms();
        return health;
    }

    [TestCase("TVClaw", EnemyType.TVClaw)]
    [TestCase("GrenadeBerserker", EnemyType.GrenadeBerserker)]
    public void PrefabAndSpawnMarkerHaveMatchingIdentity(string name, EnemyType type)
    {
        var enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(Folder + name + ".prefab");
        var marker = AssetDatabase.LoadAssetAtPath<EnemySpawnPoint>(Folder + name + "SpawnPoint.prefab");
        Assert.IsNotNull(enemy);
        Assert.IsNotNull(marker);
        Assert.AreEqual(type, enemy.Type);
        Assert.AreEqual(type, marker.Type);
        Assert.AreEqual(enemy, marker.Prefab);
        Assert.IsNotNull(enemy.GetComponent<Collider2D>());
        Assert.IsNotNull(enemy.GetComponent<EnemyAnimationDriver>());
        Assert.IsNull(enemy.GetComponent<EnemyRangedAttack>(), "No legacy bolt shooter on either new enemy.");
    }

    [Test] public void ClawIsMeleeOnlyAndScalesWithoutHealingOnDefeat()
    {
        var claw = Enemy<TVClawEnemy>("TVClaw");
        Assert.IsTrue(claw.CanMelee);
        Assert.IsFalse(claw.CanRanged);
        Assert.Greater(claw.MeleeDamage, 0);
        int health = claw.MaxHealth, damage = claw.MeleeDamage;
        claw.SetLevel(2);
        Assert.Greater(claw.MaxHealth, health);
        Assert.Greater(claw.MeleeDamage, damage);
        Assert.IsFalse(claw.TryGetShotDirection(Vector2.zero, Vector2.right, out _));
    }

    [TestCase(.8f, 15, TVClawCombatState.BiteWindup, .25f)]
    [TestCase(2.2f, 30, TVClawCombatState.SwipeWindup, .65f)]
    public void ClawAttacksHaveWindupSingleDamageAndRecovery(float distance, int damage,
        TVClawCombatState windup, float duration)
    {
        var claw = Enemy<TVClawEnemy>("TVClaw");
        var player = Player(Vector2.right * distance);
        claw.TryAcquireTarget(player.transform);
        claw.TickCombat(.01f);
        Assert.AreEqual(300, claw.MaxHealth);
        Assert.AreEqual(windup, claw.CombatState);
        Assert.AreEqual(player.MaxHealth, player.CurrentHealth);
        claw.TickCombat(duration);
        Assert.AreEqual(player.MaxHealth - damage, player.CurrentHealth);
        Assert.AreEqual(TVClawCombatState.Recovery, claw.CombatState);
        claw.TickCombat(.4f);
        Assert.AreEqual(player.MaxHealth - damage, player.CurrentHealth);
    }

    [Test] public void ClawMissesWhenPlayerEscapesAndDamageDoesNotCancelWindup()
    {
        var claw = Enemy<TVClawEnemy>("TVClaw");
        var player = Player(Vector2.right * 2.2f);
        claw.TryAcquireTarget(player.transform);
        claw.TickCombat(.01f);
        claw.TakeDamage(1);
        Assert.AreEqual(TVClawCombatState.SwipeWindup, claw.CombatState);
        player.transform.position = Vector2.right * 5;
        claw.TickCombat(.7f);
        Assert.AreEqual(player.MaxHealth, player.CurrentHealth);
        Assert.AreEqual(TVClawCombatState.Recovery, claw.CombatState);
    }

    [Test] public void PounceLocksDirectionAndMissHasRecovery()
    {
        var floor = new GameObject("Pounce floor");
        floor.transform.SetParent(root.transform);
        floor.transform.position = Vector2.down * .5f;
        floor.layer = 3;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(30, 1);
        var claw = Enemy<TVClawEnemy>("TVClaw");
        claw.GetComponent<Rigidbody2D>().simulated = true;
        var player = Player(Vector2.right * 5);
        Physics2D.SyncTransforms();
        claw.TryAcquireTarget(player.transform);
        claw.TickCombat(.71f);
        Assert.AreEqual(TVClawCombatState.PounceWindup, claw.CombatState);
        Assert.AreEqual(1, claw.LockedDirection);
        player.transform.position = Vector2.left * 5;
        claw.TickCombat(.56f);
        Assert.AreEqual(TVClawCombatState.Pouncing, claw.CombatState);
        Assert.Greater(claw.GetComponent<Rigidbody2D>().linearVelocity.x, 0);
        claw.TickCombat(.56f);
        Assert.AreEqual(TVClawCombatState.Recovery, claw.CombatState);
        Assert.AreEqual(player.MaxHealth, player.CurrentHealth);
        claw.TickCombat(.81f);
        claw.TickCombat(.71f);
        Assert.AreEqual(TVClawCombatState.Pursuing, claw.CombatState, "Cooldown prevents immediate repeat pounces.");
    }

    [TestCase(1f, false)]
    [TestCase(-1f, false)]
    [TestCase(1f, true)]
    public void BiteReachesPlayerInFrontOfMouthWithoutBodyOverlap(float direction, bool escape)
    {
        var claw = Enemy<TVClawEnemy>("TVClaw");
        var player = Player(new Vector2(direction * 2, 0));
        var collider = player.GetComponent<BoxCollider2D>();
        collider.offset = new Vector2(0, .65f);
        collider.size = new Vector2(.7f, 1.3f);
        Physics2D.SyncTransforms();
        Assert.Greater(Mathf.Abs(player.transform.position.x - claw.transform.position.x), 1.1f,
            "Old origin-distance bite would reject this standing position.");
        claw.TryAcquireTarget(player.transform);
        claw.TickCombat(.01f);
        Assert.AreEqual(TVClawCombatState.BiteWindup, claw.CombatState);
        if (escape) { player.transform.position += Vector3.right * direction * 3; Physics2D.SyncTransforms(); }
        claw.TickCombat(claw.BiteWindup);
        Assert.AreEqual(player.MaxHealth - (escape ? 0 : 15), player.CurrentHealth);
    }

    [Test] public void MouthBiteCannotReachPlayerAboveItOrThroughTerrain()
    {
        var claw = Enemy<TVClawEnemy>("TVClaw");
        var player = Player(new Vector2(2, 4));
        claw.TryAcquireTarget(player.transform);
        claw.TickCombat(.01f);
        Assert.AreEqual(TVClawCombatState.Pursuing, claw.CombatState);
        player.transform.position = new Vector2(2, 0);
        var collider = player.GetComponent<BoxCollider2D>();
        collider.offset = new Vector2(0, .65f);
        collider.size = new Vector2(.7f, 1.3f);
        var wall = new GameObject("Bite blocker");
        wall.transform.SetParent(root.transform);
        wall.layer = 3;
        wall.transform.position = new Vector2(1.45f, 1);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 2);
        Physics2D.SyncTransforms();
        claw.TickCombat(.01f);
        Assert.AreEqual(TVClawCombatState.BiteWindup, claw.CombatState);
        claw.TickCombat(claw.BiteWindup);
        Assert.AreEqual(player.MaxHealth, player.CurrentHealth);
    }

    [Test] public void DamageTriggersPermanentRageButZeroAndNegativeDoNot()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        enemy.TakeDamage(0);
        enemy.TakeDamage(-1);
        Assert.IsFalse(enemy.IsBerserk);
        enemy.TakeDamage(1);
        Assert.IsTrue(enemy.IsBerserk);
        Assert.IsFalse(enemy.GetComponent<EnemyAttackAnimations>().enabled);
        enemy.EnterBerserk();
        Assert.IsTrue(enemy.IsBerserk);
        Assert.IsFalse(enemy.CanStartAttack);
    }

    [UnityTest] public IEnumerator OneShotDeathExplodesOnceEvenWithReentrantDamageAndMultipleHurtboxes()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        var player = Player(Vector2.right * 2, true);
        int damage = Mathf.Max(enemy.MeleeDamage, enemy.RangedDamage) * 2;
        int calls = 0, defeats = 0;
        player.OnHealthChanged += (_, __) => { calls++; enemy.TakeDamage(999); };
        enemy.Defeated += _ => defeats++;
        enemy.TakeDamage(enemy.CurrentHealth);
        enemy.TakeDamage(999);
        Assert.IsTrue(enemy.HasExploded);
        Assert.AreEqual(player.MaxHealth - damage, player.CurrentHealth);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, defeats);
        yield return null;
    }

    [UnityTest] public IEnumerator NearbyPlayerTriggersRageAndContactDetonates()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        var player = Player(Vector2.right * 3);
        enemy.TryAcquireTarget(player.transform);
        yield return null;
        Assert.IsTrue(enemy.IsBerserk);
        Assert.IsFalse(enemy.IsDefeated);
        player.transform.position = Vector2.right * .5f;
        Physics2D.SyncTransforms();
        yield return null;
        Assert.IsTrue(enemy.IsDefeated);
        Assert.IsTrue(enemy.HasExploded);
        Assert.Less(player.CurrentHealth, player.MaxHealth);
    }

    [UnityTest] public IEnumerator DistantPlayerGetsGrenadeAndRageStopsFurtherThrows()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        // Acquire inside even the smallest awareness radius, then move into throw range.
        var player = Player(Vector2.right);
        Assert.IsTrue(enemy.TryAcquireTarget(player.transform));
        player.transform.position = Vector2.right * 8;
        Physics2D.SyncTransforms();
        yield return null;
        var grenades = Object.FindObjectsByType<BerserkerGrenade>();
        Assert.AreEqual(1, grenades.Length);
        Assert.Greater(grenades[0].GetComponent<Rigidbody2D>().linearVelocity.y, 0);
        enemy.TakeDamage(1);
        // Make the throw timer ready to prove rage, rather than cooldown, suppresses throws.
        typeof(GrenadeBerserkerEnemy).GetField("nextThrow",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(enemy, 0f);
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<BerserkerGrenade>().Length);
        Assert.IsFalse(enemy.HasExploded);
    }

    [UnityTest] public IEnumerator GrenadierStopsThrowingBeyondMaximumButRetainsAggroAndResumesInRange()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        // Establish aggro before moving out of range, independent of randomized awareness.
        // No frame elapses nearby, so proximity does not trigger rage.
        var player = Player(Vector2.right);
        Assert.IsTrue(enemy.TryAcquireTarget(player.transform));
        player.transform.position = Vector2.right * (enemy.MaxThrowRange + .1f);
        yield return null;
        Assert.IsTrue(enemy.HasAggro);
        Assert.IsFalse(enemy.IsBerserk);
        Assert.AreEqual(0, Object.FindObjectsByType<BerserkerGrenade>().Length);
        player.transform.position = Vector2.right * enemy.MaxThrowRange;
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<BerserkerGrenade>().Length, "Maximum boundary is included.");
        player.transform.position = Vector2.right * (enemy.MaxThrowRange + 5);
        typeof(GrenadeBerserkerEnemy).GetField("nextThrow",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(enemy, 0f);
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<BerserkerGrenade>().Length, "Aggro cannot bypass the throw limit.");
    }

    [UnityTest] public IEnumerator GrenadeSurvivesOwnerDeathAndDamagesOnceAfterFuse()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        var player = Player(Vector2.right * 6, true);
        var grenade = new GameObject("Test grenade").AddComponent<BerserkerGrenade>();
        grenade.transform.position = player.transform.position;
        grenade.Launch(player.transform.position, .2f, 2, enemy.RangedDamage);
        grenade.GetComponent<Rigidbody2D>().simulated = false;
        int damage = enemy.RangedDamage;
        enemy.TakeDamage(enemy.CurrentHealth);
        Assert.IsTrue(grenade != null);
        yield return new WaitForSeconds(.3f);
        Assert.IsTrue(grenade == null);
        Assert.AreEqual(player.MaxHealth - damage, player.CurrentHealth);
    }

    [UnityTest] public IEnumerator RagePassesThroughExistingAndNewEnemiesWithoutCrowdSlowdown()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        var ally = Enemy<TVClawEnemy>("TVClaw");
        ally.transform.position = Vector2.right * .5f;
        ally.GetComponent<Rigidbody2D>().simulated = true;
        enemy.GetComponent<Rigidbody2D>().simulated = true;
        // Crowd movement requires an acquired target. Establish it inside every
        // awareness range, then move away before any proximity-rage update.
        var player = Player(Vector2.right);
        Assert.IsTrue(enemy.TryAcquireTarget(player.transform), "Rush fixture must establish aggro.");
        player.transform.position = Vector2.right * 8;
        enemy.SetTarget(player.transform);
        Assert.IsFalse(Physics2D.GetIgnoreCollision(enemy.GetComponent<Collider2D>(), ally.GetComponent<Collider2D>()));
        enemy.EnterBerserk();
        Assert.IsTrue(Physics2D.GetIgnoreCollision(enemy.GetComponent<Collider2D>(), ally.GetComponent<Collider2D>()));
        var newcomer = Enemy<TVClawEnemy>("TVClaw");
        newcomer.transform.position = Vector2.right * .7f;
        newcomer.GetComponent<Rigidbody2D>().simulated = true;
        var body = enemy.GetComponent<Rigidbody2D>();
        body.simulated = true;
        Physics2D.SyncTransforms();
        enemy.Move();
        Assert.IsTrue(Physics2D.GetIgnoreCollision(enemy.GetComponent<Collider2D>(), newcomer.GetComponent<Collider2D>()));
        Assert.Greater(body.linearVelocity.x, 0, "A stationary ally must not clamp rush speed to zero.");
        Assert.IsFalse(Physics2D.GetIgnoreCollision(enemy.GetComponent<Collider2D>(), player.GetComponent<Collider2D>()));
        yield return null;
    }

    [UnityTest] public IEnumerator DeathBlastDamagesAnEnemyOnlyOnceAcrossMultipleColliders()
    {
        var bomber = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        var ally = Enemy<TVClawEnemy>("TVClaw");
        ally.transform.position = Vector2.right;
        ally.GetComponent<Rigidbody2D>().simulated = true;
        var extra = new GameObject("Extra enemy hurtbox");
        extra.transform.SetParent(ally.transform, false);
        extra.AddComponent<CircleCollider2D>().isTrigger = true;
        Physics2D.SyncTransforms();
        int before = ally.CurrentHealth;
        int damage = Mathf.Max(bomber.MeleeDamage, bomber.RangedDamage) * 2;
        bomber.TakeDamage(bomber.CurrentHealth);
        Assert.AreEqual(before - damage, ally.CurrentHealth);
        bomber.TakeDamage(999);
        Assert.AreEqual(before - damage, ally.CurrentHealth);
        Assert.AreEqual(1, Object.FindObjectsByType<EnemyAirBurst>().Length, "No nearby floor means a radial air burst.");
        yield return null;
    }

    [UnityTest] public IEnumerator DeathBlastCanChainAnotherBerserkerWithoutRepeatedDefeats()
    {
        var first = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        var second = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        second.transform.position = Vector2.right;
        second.GetComponent<Rigidbody2D>().simulated = true;
        second.TakeDamage(second.CurrentHealth - 1);
        int firstDefeats = 0, secondDefeats = 0;
        first.Defeated += _ => firstDefeats++;
        second.Defeated += _ => secondDefeats++;
        Physics2D.SyncTransforms();
        first.TakeDamage(first.CurrentHealth);
        Assert.IsTrue(first.HasExploded);
        Assert.IsTrue(second.HasExploded);
        Assert.AreEqual(1, firstDefeats);
        Assert.AreEqual(1, secondDefeats);
        yield return null;
    }

    [TestCase(0)]
    [TestCase(3)]
    public void ExplosionVisualFindsGroundBelowEnemies(int terrainLayer)
    {
        var floor = new GameObject("Test floor");
        floor.transform.SetParent(root.transform);
        floor.layer = terrainLayer;
        floor.transform.position = new Vector2(0, 2);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(10, 1);
        Player(new Vector2(0, 3.5f));
        Physics2D.SyncTransforms();
        Vector2 anchor = EnemyBlast.GroundVisualAnchor(new Vector2(0, 4), 3);
        Assert.AreEqual(2.5f, anchor.y, .001f, "Anchor must hit the floor top, not the player.");
    }

    [TestCase(.5f)]
    [TestCase(0f)]
    public void BottomAnchoredExplosionStaysAboveSurfaceRegardlessOfSpritePivot(float pivotY)
    {
        var texture = new Texture2D(8, 8);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, pivotY), 8);
        var effect = new GameObject("Test grounded explosion");
        effect.transform.SetParent(root.transform);
        effect.transform.position = new Vector3(3, 2.5f);
        try
        {
            effect.AddComponent<TurretBossVfxBurst>().Initialize(new[] { sprite }, new Vector2(4, 4), .65f, anchorBottom: true);
            var bounds = effect.GetComponent<SpriteRenderer>().bounds;
            Assert.AreEqual(2.5f, bounds.min.y, .001f);
            Assert.AreEqual(3f, bounds.center.x, .001f);
        }
        finally { Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
    }

    [Test] public void GroundExplosionIgnoresTransparentBottomPadding()
    {
        var texture = new Texture2D(8, 8);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * .5f, 8);
        var effect = new GameObject("Padded explosion");
        effect.transform.SetParent(root.transform);
        effect.transform.position = new Vector3(0, 2);
        try
        {
            effect.AddComponent<TurretBossVfxBurst>().Initialize(new[] { sprite }, new Vector2(4, 4),
                .65f, anchorBottom: true, transparentBottom: .125f);
            var bounds = effect.GetComponent<SpriteRenderer>().bounds;
            Assert.AreEqual(2f, bounds.min.y + bounds.size.y * .125f, .001f,
                "The visible base, rather than the transparent canvas, must touch ground.");
        }
        finally { Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
    }

    [Test] public void DistantFloorDoesNotSelectGroundExplosion()
    {
        var floor = new GameObject("Floor");
        floor.transform.SetParent(root.transform);
        floor.transform.position = Vector2.down * .5f;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(20, 1);
        Physics2D.SyncTransforms();
        Assert.IsTrue(EnemyBlast.TryGroundVisualAnchor(new Vector2(0, .5f), out var anchor));
        Assert.AreEqual(0, anchor.y, .001f);
        Assert.IsFalse(EnemyBlast.TryGroundVisualAnchor(new Vector2(0, 4), out _));
    }

    [UnityTest] public IEnumerator PlayerContactUsesRadialBurstEvenNearGround()
    {
        var floor = new GameObject("Floor");
        floor.transform.SetParent(root.transform);
        floor.transform.position = Vector2.down * .5f;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(20, 1);
        Physics2D.SyncTransforms();
        EnemyBlast.Detonate(new Vector2(0, .5f), 2, 0, forceAir: true);
        var air = Object.FindObjectsByType<EnemyAirBurst>().Single();
        Assert.AreEqual(new Vector3(0, .5f), air.transform.position);
        Assert.AreEqual(0, Object.FindObjectsByType<TurretBossVfxBurst>().Length);
        yield return null;
    }

    [UnityTest] public IEnumerator DespawningLivingBerserkerDoesNotExplode()
    {
        var enemy = Enemy<GrenadeBerserkerEnemy>("GrenadeBerserker");
        var player = Player(Vector2.right * 2);
        Object.Destroy(enemy.gameObject);
        yield return null;
        Assert.AreEqual(player.MaxHealth, player.CurrentHealth);
        Assert.AreEqual(0, Object.FindObjectsByType<EnemyBlastFlash>().Length);
    }

    [UnityTest] public IEnumerator TerrainBlocksBlastAndOutsideRadiusIsSafe()
    {
        var near = Player(Vector2.right * 2, true);
        var far = Player(Vector2.right * 5);
        var wall = new GameObject("Terrain wall");
        wall.transform.SetParent(root.transform);
        wall.layer = 3;
        wall.transform.position = Vector2.right;
        wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 5);
        Physics2D.SyncTransforms();
        EnemyBlast.Detonate(Vector2.zero, 3, 20);
        Assert.AreEqual(near.MaxHealth, near.CurrentHealth);
        Assert.AreEqual(far.MaxHealth, far.CurrentHealth);
        yield return null;
    }

    [TestCase(EnemyType.TVClaw, typeof(TVClawEnemy))]
    [TestCase(EnemyType.GrenadeBerserker, typeof(GrenadeBerserkerEnemy))]
    public void FactorySupportsNewTypesAndAwardsOneDefeat(EnemyType type, System.Type implementation)
    {
        var spawner = root.AddComponent<EnemySpawner>();
        var enemy = spawner.CreateEnemy(type);
        enemy.transform.SetParent(root.transform);
        Assert.IsInstanceOf(implementation, enemy);
        Assert.AreEqual(1, spawner.AliveCount);
        int defeats = 0;
        spawner.OnEnemyDefeated += (_, __) => defeats++;
        enemy.TakeDamage(enemy.CurrentHealth);
        enemy.TakeDamage(999);
        Assert.AreEqual(1, defeats);
        Assert.AreEqual(0, spawner.AliveCount);
    }
}
#endif
