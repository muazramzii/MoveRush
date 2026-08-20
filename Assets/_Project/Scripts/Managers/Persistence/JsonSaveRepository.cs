using System;
using System.IO;
using System.Text;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using UnityEngine;

namespace MoveRush.Managers.Persistence
{
    /// <summary>
    /// File-backed storage for the save payload. Writes are atomic: the payload goes to a
    /// temporary file first and only replaces the live file once it is fully flushed, so a crash
    /// or a battery cut mid-write can never leave a truncated profile behind. The previous file
    /// is kept as a backup and is restored automatically when the main file is unreadable.
    /// </summary>
    public class JsonSaveRepository : ISaveRepository
    {
        /// <summary>
        /// The only PlayerPrefs key in the project. It records where the profile was written so
        /// support tooling and future migrations can locate a file the player already has.
        /// </summary>
        public const string SavePathPrefsKey = "MoveRush.SavePath";

        private readonly string filePath;
        private readonly string temporaryPath;
        private readonly string backupPath;

        /// <summary>Creates a repository for a file inside the persistent data path.</summary>
        /// <param name="fileName">File name including extension.</param>
        public JsonSaveRepository(string fileName)
        {
            string safeName = string.IsNullOrWhiteSpace(fileName) ? "moverush.save.json" : fileName;
            filePath = Path.Combine(Application.persistentDataPath, safeName);
            temporaryPath = filePath + ".tmp";
            backupPath = filePath + ".bak";

            PlayerPrefs.SetString(SavePathPrefsKey, filePath);
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public string Location => filePath;

        /// <inheritdoc />
        public bool Exists() => File.Exists(filePath) || File.Exists(backupPath);

        /// <inheritdoc />
        public bool TryRead(out string payload)
        {
            if (TryReadFile(filePath, out payload))
            {
                return true;
            }

            if (TryReadFile(backupPath, out payload))
            {
                Log.Warning("SaveRepository: the main file was unreadable, the backup was restored.");
                return true;
            }

            payload = null;
            return false;
        }

        /// <inheritdoc />
        public bool Write(string payload)
        {
            if (payload == null)
            {
                Log.Error("SaveRepository: refused to write a null payload.");
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(temporaryPath, payload, new UTF8Encoding(false));

                if (File.Exists(filePath))
                {
                    File.Copy(filePath, backupPath, true);
                    File.Delete(filePath);
                }

                File.Move(temporaryPath, filePath);
                return true;
            }
            catch (Exception exception)
            {
                Log.Exception(exception, $"SaveRepository: writing '{filePath}' failed.");
                CleanUpTemporaryFile();
                return false;
            }
        }

        /// <inheritdoc />
        public bool Delete()
        {
            try
            {
                DeleteIfPresent(filePath);
                DeleteIfPresent(backupPath);
                CleanUpTemporaryFile();
                return true;
            }
            catch (Exception exception)
            {
                Log.Exception(exception, $"SaveRepository: deleting '{filePath}' failed.");
                return false;
            }
        }

        /// <summary>Reads a file, returning false instead of throwing when it cannot be read.</summary>
        /// <param name="path">Absolute path to read.</param>
        /// <param name="payload">File contents, or null on failure.</param>
        /// <returns>True when non-empty content was read.</returns>
        private static bool TryReadFile(string path, out string payload)
        {
            payload = null;

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string content = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return false;
                }

                payload = content;
                return true;
            }
            catch (Exception exception)
            {
                Log.Exception(exception, $"SaveRepository: reading '{path}' failed.");
                return false;
            }
        }

        /// <summary>Deletes a file when it exists.</summary>
        /// <param name="path">Absolute path to delete.</param>
        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>Removes a leftover temporary file after a failed write.</summary>
        private void CleanUpTemporaryFile()
        {
            try
            {
                DeleteIfPresent(temporaryPath);
            }
            catch (Exception exception)
            {
                Log.Exception(exception, "SaveRepository: removing the temporary file failed.");
            }
        }
    }
}
