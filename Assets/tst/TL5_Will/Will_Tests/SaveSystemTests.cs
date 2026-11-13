using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EscapeThe90s.MetaProgression;
using EscapeThe90s.Contracts;

public class SaveSystemTests
{
    private static readonly int TypeCount = Enum.GetValues(typeof(UpgradeType)).Length;

    private string path, tempPath, backupPath;
    private int errorCount;

    [SetUp]
    public void SetUp()
    {
        path = Path.Combine(Application.persistentDataPath, "metaprogression.json");
        tempPath = path + ".tmp";
        backupPath = path + ".test_backup";

        Directory.CreateDirectory(Application.persistentDataPath);

        // Preserve a real save, then start every test from a clean slate.
        if (File.Exists(path) && !File.Exists(backupPath)) File.Move(path, backupPath);
        DeleteAll();

        errorCount = 0;
        SaveSystem.OnPersistenceError += OnError;
    }

    [TearDown]
    public void TearDown()
    {
        SaveSystem.OnPersistenceError -= OnError;
        DeleteAll();
        if (File.Exists(backupPath)) File.Move(backupPath, path);
    }

    private void OnError(string message) => errorCount++;

    private void DeleteAll()
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(tempPath)) File.Delete(tempPath);
        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
    }

    private static void ExpectLoggedError() =>
        LogAssert.Expect(LogType.Error, new Regex(@"\[SaveSystem\]"));

    private static SaveData MakeData(int currency)
    {
        var levels = new int[TypeCount];
        for (int i = 0; i < levels.Length; i++) levels[i] = i + 1;
        return new SaveData { currency = currency, upgradeLevels = levels };
    }

    private SaveData ReadFile() => JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

    private static string LevelsJson(int count)
    {
        var parts = new string[count];
        for (int i = 0; i < count; i++) parts[i] = "1";
        return string.Join(",", parts);
    }

    // ---------------- Save ----------------

    [Test]
    public void Save_ValidData_WritesFileAndRaisesNoError()
    {
        SaveSystem.Save(MakeData(250));

        Assert.IsTrue(File.Exists(path));
        SaveData read = ReadFile();
        Assert.AreEqual(250, read.currency);
        CollectionAssert.AreEqual(MakeData(250).upgradeLevels, read.upgradeLevels);
        Assert.AreEqual(0, errorCount);
    }

    [Test]
    public void Save_NullData_RaisesErrorAndWritesNothing()
    {
        ExpectLoggedError();

        SaveSystem.Save(null);

        Assert.AreEqual(1, errorCount);
        Assert.IsFalse(File.Exists(path));
    }

    [Test]
    public void Save_ExistingFile_ReplacesContents()
    {
        SaveSystem.Save(MakeData(10));
        SaveSystem.Save(MakeData(99));

        Assert.AreEqual(99, ReadFile().currency);
        Assert.AreEqual(0, errorCount);
    }

    [Test]
    public void Save_Success_LeavesNoTempFile()
    {
        SaveSystem.Save(MakeData(5));
        SaveSystem.Save(MakeData(6));

        Assert.IsFalse(File.Exists(tempPath));
    }

    [Test]
    public void Save_WriteFailure_KeepsOriginalAndRaisesError()
    {
        SaveSystem.Save(MakeData(10));

        // A directory at the temp path makes the temp write fail on every platform.
        Directory.CreateDirectory(tempPath);
        ExpectLoggedError();

        SaveSystem.Save(MakeData(999));

        Assert.AreEqual(1, errorCount);
        Assert.AreEqual(10, ReadFile().currency);
    }

    [Test]
    public void Save_RepeatedCalls_AlwaysValidJsonAndNoTempFile()
    {
        for (int i = 0; i < 100; i++)
        {
            SaveSystem.Save(MakeData(i));

            Assert.AreEqual(i, ReadFile().currency);
            Assert.IsFalse(File.Exists(tempPath));
        }
        Assert.AreEqual(0, errorCount);
    }

    // ---------------- Load ----------------

    [Test]
    public void Load_MissingFile_ReturnsDefaultsWithoutError()
    {
        SaveData data = SaveSystem.Load();

        Assert.AreEqual(0, data.currency);
        CollectionAssert.AreEqual(new int[TypeCount], data.upgradeLevels);
        Assert.AreEqual(0, errorCount);
    }

    [Test]
    public void Load_AfterSave_RoundTripsCurrencyAndLevels()
    {
        SaveData saved = MakeData(250);
        SaveSystem.Save(saved);

        SaveData loaded = SaveSystem.Load();

        Assert.AreEqual(saved.currency, loaded.currency);
        CollectionAssert.AreEqual(saved.upgradeLevels, loaded.upgradeLevels);
        Assert.AreEqual(0, errorCount);
    }

    [Test]
    public void Load_MalformedJson_ReturnsDefaultsAndRaisesError()
    {
        File.WriteAllText(path, "not json");
        ExpectLoggedError();

        SaveData data = SaveSystem.Load();

        Assert.AreEqual(0, data.currency);
        CollectionAssert.AreEqual(new int[TypeCount], data.upgradeLevels);
        Assert.AreEqual(1, errorCount);
    }

    [Test]
    public void Load_MissingFields_ReturnsDefaultsAndRaisesError()
    {
        File.WriteAllText(path, "{}");
        ExpectLoggedError();

        SaveData data = SaveSystem.Load();

        Assert.AreEqual(0, data.currency);
        CollectionAssert.AreEqual(new int[TypeCount], data.upgradeLevels);
        Assert.AreEqual(1, errorCount);
    }

    [Test]
    public void Load_NegativeCurrency_ReturnsDefaultsAndRaisesError()
    {
        File.WriteAllText(path,
            $"{{\"currency\":-5,\"upgradeLevels\":[{LevelsJson(TypeCount)}]}}");
        ExpectLoggedError();

        SaveData data = SaveSystem.Load();

        Assert.AreEqual(0, data.currency);
        CollectionAssert.AreEqual(new int[TypeCount], data.upgradeLevels);
        Assert.AreEqual(1, errorCount);
    }

    [Test]
    public void Load_NegativeLevel_ReturnsDefaultsAndRaisesError()
    {
        var levels = new string[TypeCount];
        for (int i = 0; i < levels.Length; i++) levels[i] = "1";
        levels[0] = "-1";
        File.WriteAllText(path,
            $"{{\"currency\":50,\"upgradeLevels\":[{string.Join(",", levels)}]}}");
        ExpectLoggedError();

        SaveData data = SaveSystem.Load();

        Assert.AreEqual(0, data.currency);
        CollectionAssert.AreEqual(new int[TypeCount], data.upgradeLevels);
        Assert.AreEqual(1, errorCount);
    }

    [Test]
    public void Load_WrongLevelCount_ReturnsDefaultsAndRaisesError()
    {
        File.WriteAllText(path,
            $"{{\"currency\":50,\"upgradeLevels\":[{LevelsJson(TypeCount + 1)}]}}");
        ExpectLoggedError();

        SaveData data = SaveSystem.Load();

        Assert.AreEqual(0, data.currency);
        CollectionAssert.AreEqual(new int[TypeCount], data.upgradeLevels);
        Assert.AreEqual(1, errorCount);
    }

    [Test]
    public void Load_InvalidFile_IsLeftOnDisk()
    {
        File.WriteAllText(path, "not json");
        ExpectLoggedError();

        SaveSystem.Load();

        Assert.AreEqual("not json", File.ReadAllText(path));
    }
}
