# SaveSystemTestsDocumentation

**Project:** Escape the 90s (Unity / C# PC build)
**Feature:** TL5 – Meta Progression & Persistent Upgrade System
**Class:** `SaveSystemTests` (NUnit, Unity Test Framework, Edit Mode)
**Scope of this deliverable:** automated tests for `SaveSystem.Save(SaveData)` and `SaveSystem.Load()`

---

## 1. Description

Edit Mode tests that exercise the real save file at `Application.persistentDataPath/metaprogression.json`. They cover the Persistence round-trip, failure reporting, no-corruption-on-failure, and Stress checks from the Save and Load documentation.

`SetUp` moves any existing save to a `.test_backup` file and clears the slate. `TearDown` unsubscribes from `OnPersistenceError`, deletes test files, and restores the backup, so a developer's real save is not lost. Tests that expect `Debug.LogError` use `LogAssert.Expect`, since Unity fails a test on unexpected error logs.

### Tests

| Test | Checks |
| --- | --- |
| `Save_ValidData_WritesFileAndRaisesNoError` | File written with correct currency and levels; no event. |
| `Save_NullData_RaisesErrorAndWritesNothing` | Event fires; no file created. |
| `Save_ExistingFile_ReplacesContents` | Second save overwrites the first. |
| `Save_Success_LeavesNoTempFile` | No `.tmp` file remains. |
| `Save_WriteFailure_KeepsOriginalAndRaisesError` | A directory is placed at the temp path to force a write failure; original save unchanged; event fires. |
| `Save_RepeatedCalls_AlwaysValidJsonAndNoTempFile` | 100 saves in a loop; file always valid, no temp file. |
| `Load_MissingFile_ReturnsDefaultsWithoutError` | Defaults returned; no event (first launch is normal). |
| `Load_AfterSave_RoundTripsCurrencyAndLevels` | Saved values equal loaded values. |
| `Load_MalformedJson_ReturnsDefaultsAndRaisesError` | `not json` gives defaults and an event. |
| `Load_MissingFields_ReturnsDefaultsAndRaisesError` | `{}` gives defaults and an event. |
| `Load_NegativeCurrency_...` | Rejected; defaults; event. |
| `Load_NegativeLevel_...` | Rejected; defaults; event. |
| `Load_WrongLevelCount_...` | One extra level is rejected; defaults; event. |
| `Load_InvalidFile_IsLeftOnDisk` | `Load` does not modify a rejected file. |

---

## 2. Setup

1. In Unity, install **Test Framework** (Package Manager) if it is not already present.
2. Create an Edit Mode test folder: Test Runner window > Edit Mode > *Create EditMode Test Assembly Folder*, e.g. `Assets/Tests/EditMode/`.
3. Because `SaveSystem` is in the default `Assembly-CSharp`, which test assemblies cannot reference, put the MetaProgression scripts in an assembly definition (e.g. `EscapeThe90s.MetaProgression.asmdef`) and add it to the test assembly's **Assembly Definition References**.
4. Copy `SaveSystemTests.cs` into the test folder.

---

## 3. Usage

Open **Window > General > Test Runner**, choose **EditMode**, and click **Run All** (or run a single test).

---

## 4. Notes and Limits

- The expected level count follows `Enum.GetValues(typeof(UpgradeType)).Length`, so the tests adapt automatically when an `UpgradeType` is added (currently 4, including `Attack`). The `SaveSystemDocumentation.md` Load section still lists three types and is out of date.
- The failure test forces a write error with a directory at `metaprogression.json.tmp`. This works on all platforms, unlike a read-only file, which behaves differently on Windows and macOS/Linux.
- Max-level clamping is not tested here; it belongs to `MetaProgressionManager.RestoreSnapshot`.
- The tests touch the real save location. They back it up and restore it, but a hard crash of the editor mid-test could leave a `.test_backup` file; the next run will not overwrite it.
- **Not run in Unity:** this code has not been compiled or tested in this environment.
- The system diagram image and Champion Document were not available in the conversation; the tests follow the contract wording quoted in `SaveSystemDocumentation.md`.

---

## 5. AI Interaction Log

| # | Request | Result |
| --- | --- | --- |
| 1 | Create Unity test cases for the `SaveSystem` Load and Save functions, with succinct code and a matching documentation file. | `SaveSystemTests.cs` (14 Edit Mode tests) and this document. Covers valid, null, replace, temp-file cleanup, forced-failure, stress, missing-file, round-trip, malformed, missing-field, negative-value and wrong-count cases. |