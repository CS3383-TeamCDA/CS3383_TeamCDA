using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace EscapeThe90s.MetaProgression
{
    /// <summary>
    /// «static» «Caretaker» (Memento pattern) for TL5 Meta Progression.
    /// Owns all file I/O for the persistent save file.
    /// Contains Save() and Load() (plus the members they need).
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "metaprogression.json";

        // Class diagram: - filePath : string
        // Built lazily because Application.persistentDataPath is only safe to read at runtime.
        private static string filePath;
        private static string FilePath =>
            filePath ??= Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// Interface contract event: OnPersistenceError(string message).
        /// Raised whenever Save cannot complete so callers/UI never claim a failed save succeeded.
        /// </summary>
        public static event Action<string> OnPersistenceError;

        /// <summary>
        /// Serializes currency and upgrade levels to the local save file.
        /// Writes to a temporary file first, then swaps it in, so a crash or
        /// disk error mid-write can never corrupt an existing valid save.
        /// On any failure the existing save is left untouched, the in-memory
        /// state is not modified, and OnPersistenceError is raised.
        /// </summary>
        /// <param name="data">The SaveData memento to persist (must not be null).</param>
        public static void Save(SaveData data)
        {
            if (data == null)
            {
                ReportError("Save failed: SaveData was null.");
                return;
            }

            string path = FilePath;
            string tempPath = path + ".tmp";

            try
            {
                string json = JsonUtility.ToJson(data, true);

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 1. Write the complete new save to a temp file.
                File.WriteAllText(tempPath, json);

                // 2. Swap it in atomically (replace if a save already exists).
                Swap(tempPath, path);
            }
            catch (Exception e) when (e is IOException ||
                                      e is UnauthorizedAccessException ||
                                      e is System.Security.SecurityException ||
                                      e is ArgumentException ||
                                      e is NotSupportedException)
            {
                TryDeleteTemp(tempPath);
                ReportError($"Save failed: {e.Message}");
            }
        }

        /// <summary>
        /// Reads and validates the local save file. The file is untrusted input:
        /// currency must be non-negative and there must be one non-negative level
        /// per UpgradeType. If the file is missing, unreadable, or invalid, safe
        /// default progress is returned and OnPersistenceError is raised (a missing
        /// file on first launch is not an error and is not reported).
        /// </summary>
        /// <returns>Validated SaveData, or default progress (0 currency, all levels 0).</returns>
        public static SaveData Load()
        {
            int typeCount = Enum.GetValues(typeof(UpgradeType)).Length;
            string path = FilePath;

            if (!File.Exists(path))
            {
                return Defaults(typeCount);
            }

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

                if (data != null && data.currency >= 0 && data.upgradeLevels != null
                    && data.upgradeLevels.Length == typeCount
                    && Array.TrueForAll(data.upgradeLevels, l => l >= 0))
                {
                    return data;
                }

                ReportError("Load failed: save data failed validation; using defaults.");
            }
            catch (Exception e)
            {
                ReportError($"Load failed: {e.Message}; using defaults.");
            }

            return Defaults(typeCount);
        }

        // ---- Helpers ----

        private static SaveData Defaults(int typeCount) =>
            new SaveData { currency = 0, upgradeLevels = new int[typeCount] };

        private static void ReportError(string message)
        {
            Debug.LogError($"[SaveSystem] {message}");
            OnPersistenceError?.Invoke(message);
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch (Exception)
            {
                // Best-effort cleanup only; the original error is what gets reported.
            }
        }

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
    }
}