# SaveSystemDocumentation

**Project:** Escape the 90s (Unity / C# PC build)
**Feature:** TL5 – Meta Progression & Persistent Upgrade System
**Class:** `SaveSystem` (`«static» «Caretaker»`)
**Scope of this deliverable:** the `Save(SaveData data)` function only

---

## 1. Description

`SaveSystem` is the Caretaker in the Facade / Strategy / Memento hybrid design. It is a static helper that owns all file access for the persistent save file. `MetaProgressionManager` (the Originator) creates a `SaveData` memento via `CreateSnapshot()` and hands it to `SaveSystem.Save()`; `SaveSystem` never reads or changes gameplay state itself.

`Save(SaveData data)` implements the interface contract from the Champion Document: *"Serializes currency and upgrade levels to the local save file; reports failure if the write cannot complete."*

### What `Save` does

1. Rejects a `null` snapshot and reports the error.
2. Serializes the `SaveData` (currency and `upgradeLevels[]`) to JSON with Unity's `JsonUtility`.
3. Creates the save directory if it does not exist.
4. Writes the JSON to a temporary file (`metaprogression.json.tmp`).
5. Swaps the temp file into place as `metaprogression.json` (`File.Replace` if a save already exists, otherwise `File.Move`).
6. On any I/O failure, deletes the temp file, leaves the previous save untouched, logs the error, and raises `OnPersistenceError`.

The write-to-temp-then-swap approach means a crash or full disk mid-write cannot corrupt an existing valid save, which supports the Stress and Persistence acceptance tests (no corrupted data after repeated save/load).

### Members included

| Member | Purpose |
| --- | --- |
| `static void Save(SaveData data)` | The requested function (matches the class diagram signature). |
| `private static string filePath` | From the class diagram. Built lazily as `Application.persistentDataPath/metaprogression.json`. |
| `static event Action<string> OnPersistenceError` | From Interface Contract §3.1. Needed so `Save` can report failure while keeping the diagram's `void` return type. |
| `ReportError`, `TryDeleteTemp` (private) | Small helpers used only by `Save`. |

`Load()` is intentionally **not** included.

---

## 2. Requirements

- Unity (C#, .NET Standard 2.1 or later profile; uses `UnityEngine.JsonUtility`).
- The project's existing `SaveData.cs` (the Memento). It is marked `[Serializable]` and keeps `currency` and `upgradeLevels` as `[SerializeField]` private fields, which `JsonUtility` serializes correctly, so no changes to `SaveData` are needed.

---

## 3. Installation

1. Copy `SaveSystem.cs` into your Unity project, e.g. `Assets/Scripts/MetaProgression/`.
2. Make sure `SaveData.cs` is in the project (it is, from the earlier TL5 work).
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
| Disk full, file locked, permission denied, invalid path | Existing save untouched; temp file removed; error logged; `OnPersistenceError` raised. |
| Existing save present | Replaced only after the new file is fully written. |

`Save` does not validate gameplay values (for example non-negative currency). Per the Champion Document §8, the save file is treated as untrusted input, so validation belongs in `Load()`.

---

## 6. Testing Notes (maps to Champion Document §7)

| Test | How to check |
| --- | --- |
| Persistence round-trip | Call `Save`, then (once `Load` exists) `Load` and compare currency and levels. |
| Failure reporting | Subscribe to `OnPersistenceError`, call `Save(null)`, confirm the event fires. |
| No corruption on failure | Make the save file read-only, call `Save`, confirm the original file is unchanged and the event fires. |
| Stress | Call `Save` repeatedly in a loop; confirm the file is always valid JSON and no `.tmp` file remains. |

---

## 7. Design Notes

- **Pattern role:** Caretaker in the Memento pattern. It stores and retrieves the snapshot without inspecting or modifying its contents.
- **Single responsibility:** only `SaveSystem` writes the save file; no other feature does (Champion Document §8).
- **Diagram vs. document:** the Champion Document refers to a `SaveDataManager` singleton, while the class diagram uses a static `SaveSystem`. This code follows the class diagram, which notes that Singleton was dropped and `SaveSystem` is a static helper.
- **Not run in Unity:** this code has not been compiled or tested inside Unity in this environment.