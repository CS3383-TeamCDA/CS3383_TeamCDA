using System;
using System.IO;
using UnityEngine;

namespace EscapeThe90s.MetaProgression
{
    /// <summary>
    /// «static» «Caretaker» (Memento pattern) for TL5 Meta Progression.
    /// Owns all file I/O for the persistent save file.
    /// This file contains only the Save() function (plus the members it needs).
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
                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
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

        // ---- Helpers used only by Save() ----

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
    }
}
