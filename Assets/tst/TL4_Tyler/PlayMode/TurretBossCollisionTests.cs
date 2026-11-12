using NUnit.Framework;
using UnityEngine;

public sealed class TurretBossCollisionTests
{
    [TestCase(TurretBossAttackState.FloorWarning)]
    [TestCase(TurretBossAttackState.GrenadeWarning)]
    [TestCase(TurretBossAttackState.GrenadeFlight)]
    [TestCase(TurretBossAttackState.MissileWarning)]
    [TestCase(TurretBossAttackState.MissileDescent)]
    [TestCase(TurretBossAttackState.BeamCharge)]
    [TestCase(TurretBossAttackState.ShortChargeWarning)]
    [TestCase(TurretBossAttackState.LongChargeWarning)]
    public void TelegraphsAndAirbornePayloadsNeverDamage(TurretBossAttackState state)
    {
        GameObject root = new GameObject("Warning contact test");
        Sprite square = null;
        try
        {
            PlayerHealth player = Player(root, out BoxCollider2D hurtbox);
            square = Square();
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(player, square);
            field.SetState(state, 0f, 10);
            field.Advance(0.1f);
            Assert.AreEqual(100, player.CurrentHealth);
        }
        finally { Object.DestroyImmediate(root); if (square != null) { Object.DestroyImmediate(square); } }
    }

    [Test]
    public void ChildHurtboxesCrouchTriggersAndAttackCancellationAreRespected()
    {
        GameObject root = new GameObject("Compound player test");
        Sprite square = null;
        try
        {
            PlayerHealth player = Player(root, out BoxCollider2D hurtbox);
            square = Square();
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(player, square);
            field.SetState(TurretBossAttackState.BeamFire, 0f, 10);
            field.Advance(0.01f);
            field.Advance(0.01f);
            Assert.AreEqual(90, player.CurrentHealth, "A child hurtbox must take exactly one beam hit.");
            player.ResetHealth();
            hurtbox.size = new Vector2(0.7f, 0.7f);
            hurtbox.offset = new Vector2(0f, -0.45f);
            field.SetState(TurretBossAttackState.BeamFire, 0f, 10);
            field.Advance(0.01f);
            Assert.AreEqual(100, player.CurrentHealth, "Crouch changes must apply without waiting a physics tick.");
            hurtbox.size = new Vector2(0.7f, 1.6f);
            hurtbox.offset = Vector2.zero;
            hurtbox.isTrigger = true;
            field.SetState(TurretBossAttackState.BeamFire, 0f, 10);
            field.Advance(0.01f);
            Assert.AreEqual(100, player.CurrentHealth, "Trigger sensors are not physical player hurtboxes.");
            hurtbox.isTrigger = false;
            field.SetState(TurretBossAttackState.GrenadeBlast, 0f, 10);
            field.Advance(0.01f);
            field.Advance(0.01f);
            Assert.AreEqual(90, player.CurrentHealth);
            player.ResetHealth();
            field.SetState(TurretBossAttackState.MissileWarning, 8f, 10);
            player.transform.position = new Vector3(8f, 0.8f, 0f);
            field.SetState(TurretBossAttackState.MissileBlast, 8f, 10);
            field.Advance(0.01f);
            Assert.AreEqual(90, player.CurrentHealth, "Payloads must reach the newly accessible right side of the arena.");
            player.ResetHealth();
            field.Clear();
            field.Advance(1f);
            Assert.AreEqual(100, player.CurrentHealth);
            player.transform.position = new Vector3(0f, 0.8f, 0f);
            field.SetBossOrigin(new Vector2(7f, 0f), -1f, 0f);
            field.FireFloorShot(10);
            field.Advance(1f);
            Assert.AreEqual(90, player.CurrentHealth, "A fast bolt must sweep through a child hurtbox.");
        }
        finally { Object.DestroyImmediate(root); if (square != null) { Object.DestroyImmediate(square); } }
    }

    [Test]
    public void SolidBossBlocksPlayerAndCannotDashThroughThem()
    {
        GameObject root = new GameObject("Solid boss test");
        Sprite square = null;
        SimulationMode2D previousMode = Physics2D.simulationMode;
        try
        {
            PlayerHealth player = Player(root, out BoxCollider2D hurtbox);
            player.transform.position = new Vector3(-2f, 0.8f, 0f);
            Rigidbody2D playerBody = player.gameObject.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 0f;
            playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            GameObject enemy = new GameObject("Solid boss");
            enemy.transform.SetParent(root.transform);
            square = Square();
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(player, square);
            field.enabled = false;
            TurretBossController boss = enemy.AddComponent<TurretBossController>();
            boss.Begin(player.transform, field, 1);
            boss.enabled = false;
            TurretBossBodyCollision solid = enemy.AddComponent<TurretBossBodyCollision>();
            solid.Initialize(boss, player, 0f);
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int i = 0; i < 80; i++)
            {
                playerBody.linearVelocity = Vector2.right * 6f;
                Physics2D.SyncTransforms();
                Physics2D.Simulate(0.02f);
            }
            Assert.LessOrEqual(hurtbox.bounds.max.x, solid.Shape.bounds.min.x + 0.025f,
                "Walking into the boss must stop at its solid body.");
            Assert.IsFalse(Physics2D.GetIgnoreCollision(solid.Shape, hurtbox));
            Vector3 limited = solid.ConstrainMove(new Vector3(-5f, 0.9f, 0f));
            Assert.Greater(limited.x, playerBody.position.x, "Boss charge must stop on its original side of the player.");
            enemy.transform.position = new Vector3(0f, 0.9f, 0f);
            solid.SyncShape();
            Physics2D.SyncTransforms();
            Assert.AreEqual(0f, solid.Shape.bounds.min.y, 0.001f,
                "Cosmetic phase-two lift must not lift collision off the floor.");
        }
        finally
        {
            Physics2D.simulationMode = previousMode;
            Object.DestroyImmediate(root);
            if (square != null) { Object.DestroyImmediate(square); }
        }
    }

    [TestCase(TurretBossAttackState.ShortCharge)]
    [TestCase(TurretBossAttackState.LongCharge)]
    public void BossChargesUseNonzeroMeleeDamage(TurretBossAttackState state)
    {
        GameObject root = new GameObject("Charge damage regression");
        Sprite square = Square();
        try
        {
            PlayerHealth player = Player(root, out BoxCollider2D hurtbox);
            player.transform.position = new Vector3(1.15f, 0.8f, 0f);
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(player, square);
            GameObject actor = new GameObject("Boss");
            actor.transform.SetParent(root.transform);
            TurretBossController boss = actor.AddComponent<TurretBossController>();
            boss.SetAttackMode(EnemyAttackMode.RangedOnly);
            boss.Begin(player.transform, field, 1);
            Assert.Greater(boss.MeleeDamage, 0);
            field.SetBossOrigin(Vector2.zero, 1f, 3f);
            field.SetState(state, 1.15f, boss.MeleeDamage);
            if (state == TurretBossAttackState.ShortCharge) { field.ResolveShortCharge(0f, 0f, boss.MeleeDamage); }
            else { field.ResolveLongCharge(0f, 0f, boss.MeleeDamage); }
            Assert.Less(player.CurrentHealth, 100);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(square); }
    }

    [Test]
    public void MobileLowBoltsLeaveCurrentBossPosition()
    {
        GameObject root = new GameObject("Mobile bolt regression");
        Sprite square = Square();
        try
        {
            PlayerHealth player = Player(root, out BoxCollider2D hurtbox);
            TurretBossAttackField field = root.AddComponent<TurretBossAttackField>();
            field.Initialize(player, square);
            field.SetMobileFloorOrigin(true);
            field.SetBossOrigin(new Vector2(-3f, 0.9f), 1f, 0f);
            field.FireFloorShot(10);
            field.Advance(0.3f);
            Assert.AreEqual(90, player.CurrentHealth);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(square); }
    }

    private static PlayerHealth Player(GameObject parent, out BoxCollider2D hurtbox)
    {
        GameObject player = new GameObject("Player with child hurtbox");
        player.transform.SetParent(parent.transform);
        player.transform.position = new Vector3(0f, 0.8f, 0f);
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        GameObject child = new GameObject("Body hurtbox");
        child.transform.SetParent(player.transform, false);
        hurtbox = child.AddComponent<BoxCollider2D>();
        hurtbox.size = new Vector2(0.7f, 1.6f);
        return health;
    }

    private static Sprite Square() => Sprite.Create(Texture2D.whiteTexture,
        new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
}
