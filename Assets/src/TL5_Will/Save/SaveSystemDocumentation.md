# SaveSystemDocumentation

**Project:** Escape the 90s (Unity / C# PC build)
**Feature:** TL5 – Meta Progression & Persistent Upgrade System
**Class:** `SaveSystem` (`«static» «Caretaker»`)
**Scope of this deliverable:** the `Save(SaveData data)` function and its `Swap` helper

---

## 1. Description

`SaveSystem` is the Caretaker in the Facade / Strategy / Memento hybrid design. It is a static helper that owns all file access for the persistent save file. `MetaProgressionManager` (the Originator) creates a `SaveData` memento via `CreateSnapshot()` and hands it to `SaveSystem.Save()`; `SaveSystem` never reads or changes gameplay state itself.

`Save(SaveData data)` implements the interface contract from the Champion Document: *"Serializes currency and upgrade levels to the local save file; reports failure if the write cannot complete."*

### What `Save` does

1. Rejects a `null` snapshot and reports the error.
2. Serializes the `SaveData` (currency and `upgradeLevels[]`) to JSON with Unity's `JsonUtility`.
3. Creates the save directory if it does not exist.
4. Writes the JSON to a temporary file (`metaprogression.json.tmp`).
5. Calls `Swap` to move the temp file into place as `metaprogression.json`.
6. On any I/O failure, deletes the temp file, leaves the previous save untouched, logs the error, and raises `OnPersistenceError`.

The write-to-temp-then-swap approach means a crash or full disk mid-write cannot corrupt an existing valid save, which supports the Stress and Persistence acceptance tests (no corrupted data after repeated save/load).

### What `Swap` does

`Swap(string tempPath, string path)` is a private helper that puts the finished temp file in place of the save file:

1. If a save already exists, calls `File.Replace(tempPath, path, null)`; otherwise calls `File.Move(tempPath, path)`.
2. If that throws an `IOException`, waits 25 ms and tries again, for up to 5 attempts in total.
3. If the fifth attempt also fails, the `IOException` is rethrown and handled by `Save`'s existing `catch` (temp file deleted, original save kept, error logged, `OnPersistenceError` raised).

**Why it exists:** on Windows, `File.Replace` can fail with *"Unable to remove the file to be replaced"* when another process (antivirus, search indexer, the Unity file watcher) briefly holds the save file open right after it was written. The lock clears within milliseconds, so a short retry removes the failure without changing the error contract. It was added after the repeated-save stress test failed intermittently.

```csharp
private static void Swap(string tempPath, string path)
{
    for (int attempt = 0; ; attempt++)
    {
        try
        {
            if (File.Exists(path)) File.Replace(tempPath, path, null);
            else File.Move(tempPath, path);
            return;
        }
        catch (IOException) when (attempt < 4)
        {
            Thread.Sleep(25); // transient lock; wait and retry
        }
    }
}
```

### Members included

| Member | Purpose |
| --- | --- |
| `static void Save(SaveData data)` | The requested function (matches the class diagram signature). |
| `private static string filePath` | From the class diagram. Built lazily as `Application.persistentDataPath/metaprogression.json`. |
| `static event Action<string> OnPersistenceError` | From Interface Contract §3.1. Needed so `Save` can report failure while keeping the diagram's `void` return type. |
| `Swap` (private) | Moves the temp file into place, retrying briefly on transient file locks. |
| `ReportError`, `TryDeleteTemp` (private) | Small helpers used only by `Save`. |

`Load()` is documented separately below.

---

## 2. Requirements

- Unity (C#, .NET Standard 2.1 or later profile; uses `UnityEngine.JsonUtility`).
- `using System.Threading;` in `SaveSystem.cs` for `Thread.Sleep` (part of the .NET base library; no package or assembly reference needed).
- The project's `SaveData` class (the Memento), with public fields so `JsonUtility` can serialize it:

```csharp
[System.Serializable]
public class SaveData
{
    public int currency;
    public int[] upgradeLevels;
}
```

---

## 3. Installation

1. Copy `SaveSystem.cs` into your Unity project, e.g. `Assets/Scripts/MetaProgression/`.
2. Make sure `SaveData` (above) is in the project.
3. No scene setup is needed: `SaveSystem` is static and is not attached to a GameObject.

---

## 4. Usage

### Saving progress

```csharp
SaveData snapshot = CreateSnapshot();   // MetaProgressionManager (Originator)
SaveSystem.Save(snapshot);              // SaveSystem (Caretaker)
```

### Typical call from `MetaProgressionManager.PurchaseUpgrade`

This matches step 7 of the Purchase Upgrade use case (PU1). Once the purchase has succeeded in memory:

```csharp
SaveSystem.Save(CreateSnapshot());   // step 7: persist the new state
```

### Handling save failures (use case step 7a)

`Save` returns `void`, so failure is reported through the `OnPersistenceError` event. Subscribe once, for example in the shop UI or manager:

```csharp
void OnEnable()  => SaveSystem.OnPersistenceError += HandleSaveError;
void OnDisable() => SaveSystem.OnPersistenceError -= HandleSaveError;

void HandleSaveError(string message)
{
    // In-memory progress is preserved; just tell the player the save did not complete.
    saveStatusText.text = "Progress could not be saved.";
}
```

### Save file location

`Application.persistentDataPath/metaprogression.json`. On Windows this is typically
`%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\metaprogression.json`.

Example file contents:

```json
{
    "currency": 250,
    "upgradeLevels": [
        2,
        1,
        0
    ]
}
```

---

## 5. Behaviour and Error Handling

| Situation | Result |
| --- | --- |
| Valid `SaveData` | File written; no event raised. |
| `data == null` | Nothing written; error logged; `OnPersistenceError` raised. |
| Directory missing | Created automatically. |
| Save file briefly locked by another process | `Swap` retries (up to 5 attempts, 25 ms apart); save succeeds once the lock clears; no event raised. |
| Lock persists, disk full, permission denied, invalid path | Existing save untouched; temp file removed; error logged; `OnPersistenceError` raised. |
| Existing save present | Replaced only after the new file is fully written. |

Only `IOException` is retried. Errors such as `UnauthorizedAccessException` fail immediately, since waiting will not fix them.

`Save` does not validate gameplay values (for example non-negative currency). Per the Champion Document §8, the save file is treated as untrusted input, so validation belongs in `Load()`.

---

## 6. Testing Notes (maps to Champion Document §7)

| Test | How to check |
| --- | --- |
| Persistence round-trip | Call `Save`, then `Load`, and compare currency and levels. |
| Failure reporting | Subscribe to `OnPersistenceError`, call `Save(null)`, confirm the event fires. |
| No corruption on failure | Create a folder named `metaprogression.json.tmp` so the temp write fails, call `Save`, confirm the original file is unchanged and the event fires. |
| Stress | Call `Save` repeatedly in a loop; confirm the file is always valid JSON and no `.tmp` file remains. Without `Swap`'s retry this test can fail intermittently on Windows. |

---

## 7. Design Notes

- **Pattern role:** Caretaker in the Memento pattern. It stores and retrieves the snapshot without inspecting or modifying its contents.
- **Single responsibility:** only `SaveSystem` writes the save file; no other feature does (Champion Document §8).
- **Diagram vs. document:** the Champion Document refers to a `SaveDataManager` singleton, while the class diagram uses a static `SaveSystem`. This code follows the class diagram, which notes that Singleton was dropped and `SaveSystem` is a static helper.
- **Main-thread pause:** `Swap` uses `Thread.Sleep`, so a save that keeps hitting a lock can pause the main thread for about 100 ms (4 waits × 25 ms) before it gives up. A coroutine or async retry would avoid this but would make `Save` no longer synchronous.
- **Not run in Unity:** the `Swap` change has not been compiled or tested in this environment.


---
---

# Load Function (added)

**Scope of this addition:** the `Load()` function in `SaveSystem`.

## L1. Description

`Load()` implements the interface contract: *"Returns validated saved data; if no valid file exists, returns default progress without crashing."* It reads `metaprogression.json` (the file `Save` writes), deserializes it with `JsonUtility`, validates it, and returns a `SaveData`.

Per Champion Document §8, the save file is treated as untrusted input. Data is accepted only if:

- `currency` is non-negative, and
- `upgradeLevels` has exactly one entry per `UpgradeType` (Health, Speed, MagnetRange), and
- every level is non-negative.

Otherwise safe default progress is returned: currency `0` and all levels `0`.

## L2. Usage

```csharp
// Run start (use case RS1, step 3) or game start
SaveData data = SaveSystem.Load();
```

Subscribe to `OnPersistenceError` if you want to be told when defaults were substituted:

```csharp
SaveSystem.OnPersistenceError += message => Debug.LogWarning(message);
```

`Load` never throws and never returns `null`, so the result can be used directly, for example in `MetaProgressionManager.RestoreSnapshot(data)`. Default progress is built with the object initializer `new SaveData { currency = 0, upgradeLevels = new int[n] }`, where `n` is the number of `UpgradeType` values.

## L3. Behaviour

| Situation | Result |
| --- | --- |
| Valid save file | Returns the saved `SaveData`; no event raised. |
| File does not exist (first launch) | Returns default progress; no event raised, because this is normal. |
| Unreadable file, malformed JSON, or missing fields | Returns default progress; error logged; `OnPersistenceError` raised. |
| Negative currency or level, or wrong number of levels | Returns default progress; error logged; `OnPersistenceError` raised. |

## L4. Notes and Limits

- **Maximum level is not checked here.** Max levels live in each `UpgradeDefinition`, which the static `SaveSystem` does not know about. Clamping levels to their maximum belongs in `MetaProgressionManager.RestoreSnapshot`.
- **Adding an `UpgradeType`:** the expected level count follows the enum automatically. Saves written before the new type was added will fail validation and fall back to defaults.
- **Load does not write.** A rejected file is left on disk and is overwritten by the next `Save`.

## L5. Testing Notes (maps to Champion Document §7)

| Test | How to check |
| --- | --- |
| Round-trip | `Save` a snapshot, `Load`, and compare currency and every level. |
| Missing file | Delete the save file; `Load` returns `0` currency, all levels `0`, with no event. |
| Corrupt file | Replace the file contents with `not json`; `Load` returns defaults and `OnPersistenceError` fires. |
| Invalid values | Edit the file to `"currency": -5`; `Load` returns defaults and the event fires. |

---
---

# AI Interaction Log

| # | Request | Result |
| --- | --- | --- |
| 1 | Asked for only the `Save` function in `SaveSystem`, based on the class diagram and champion document, plus a README named `SaveSystemDocumentation`. | Created `SaveSystem.cs` with `Save` (temp-file write then swap, null check, `OnPersistenceError` event) and the first version of this document. |
| 2 | Asked for the `Load` function, succinct with no extra features, with its documentation added to the previous file. | Added `Load` (validates currency and levels, returns default progress on a missing or invalid file) and the Load Function section. |
| 3 | Supplied the actual `SaveData` class (public `currency` and `upgradeLevels` fields, no constructor). | Found that `Load` used a constructor and properties `SaveData` does not have; rewrote it to use the public fields, added null checks and a `Defaults` helper, and updated this document. |
| 4 | Asked what the `??=` notation means. | Explained that it is C#'s null-coalescing assignment operator and how `SaveSystem` uses it to build `filePath` lazily. |
| 5 | Stress test `Save_RepeatedCalls_AlwaysValidJsonAndNoTempFile` failed with *"Unable to remove the file to be replaced"*. | Diagnosed a transient Windows file lock on `File.Replace`; added the `Swap` helper with up to 5 attempts, 25 ms apart. |
| 6 | Asked what dependencies `Thread` needs. | Only `using System.Threading;`; noted the ~100 ms worst-case main-thread pause. |
| 7 | Asked to document `Swap`. | Updated this document (Save steps, `Swap` section, members, behaviour, testing and design notes). |