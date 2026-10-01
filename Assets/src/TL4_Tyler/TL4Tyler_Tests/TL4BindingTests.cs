using NUnit.Framework;
using UnityEngine;

public class TL4BindingTests
{
    private GameObject spawnerObject;
    private EnemyBase enemy1;
    private EnemyBase enemy2;

    [SetUp]
    public void SetUp()
    {
        spawnerObject = new GameObject("Test Enemy Spawner");
        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.SetSpawnRate(5.0f);

        enemy1 = spawner.CreateEnemy(EnemyType.Basic);
        enemy1.name = "Enemy 1";
        enemy2 = spawner.CreateEnemy(EnemyType.Basic);
        enemy2.name = "Enemy 2";
    }

    [TearDown]
    public void TearDown()
    {
        if (enemy1 != null)
        {
            Object.DestroyImmediate(enemy1.gameObject);
        }

        if (enemy2 != null)
        {
            Object.DestroyImmediate(enemy2.gameObject);
        }

        if (spawnerObject != null)
        {
            Object.DestroyImmediate(spawnerObject);
        }
    }

    [Test]
    public void BaseRef_ToDifferentEnemy_AttackUsesCurrentEnemy()
    {
        EnemyBase current = enemy1;
        Assert.AreEqual("Enemy 1 attacks", current.Attack());

        current = enemy2;
        Assert.AreEqual("Enemy 2 attacks", current.Attack());

        current = enemy1;
        Assert.AreEqual("Enemy 1 attacks", current.Attack());
    }

    [Test]
    public void BaseRef_ToSameEnemy_AttackDoesNotChange()
    {
        EnemyBase baseline = enemy1;
        EnemyBase current = enemy1;

        Assert.AreEqual("Enemy 1 attacks", current.Attack());
        Assert.AreEqual(baseline.Attack(), current.Attack());
    }

    [Test]
    public void DynamicBinding_BaseReference_UsesSubclassOverride()
    {
        EnemyBase current = spawnerObject.AddComponent<TL4OverrideEnemy>();

        Assert.AreEqual("Override attack", current.Attack());
        Assert.AreNotEqual(spawnerObject.name + " attacks", current.Attack());
    }

    [Test]
    public void DynamicBinding_HiddenMethod_DoesNotOverrideBaseMethod()
    {
        TL4HiddenAttackEnemy derived = spawnerObject.AddComponent<TL4HiddenAttackEnemy>();
        EnemyBase current = derived;

        Assert.AreEqual("Hidden attack", derived.Attack());
        Assert.AreEqual(spawnerObject.name + " attacks", current.Attack());
    }

    [Test]
    [Explicit("Intentional failure: run individually to demonstrate an incorrect dynamic binding expectation.")]
    public void DynamicBinding_FailDemo_ExpectsBaseInsteadOfOverride()
    {
        EnemyBase current = spawnerObject.AddComponent<TL4OverrideEnemy>();

        // Intentionally wrong: the runtime type selects the override, even through EnemyBase.
        Assert.AreEqual(spawnerObject.name + " attacks", current.Attack());
    }
}
