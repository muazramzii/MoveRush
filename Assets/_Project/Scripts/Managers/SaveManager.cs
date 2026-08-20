using System;
using System.Collections;
using MoveRush.Core.Config;
using MoveRush.Core.Data;
using MoveRush.Core.Services;
using MoveRush.Core.Utilities;
using MoveRush.Managers.Persistence;
using UnityEngine;

namespace MoveRush.Managers
{
    /// <summary>
    /// JSON-backed persistence service. It owns the in-memory player profile, serialises it
    /// through <see cref="ISaveRepository"/> and guarantees the profile is flushed when the
    /// application is backgrounded or quit - the two moments a mobile process is most likely to
    /// be killed. Storage mechanics live in the repository; this class only decides when and
    /// what to persist.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveManager : MonoBehaviour, ISaveService, IGameService
    {
        private IConfigProvider configProvider;
        private ISaveRepository repository;
        private Coroutine autoSaveRoutine;
        private bool isDirty;

        /// <inheritdoc />
        public int InitializationOrder => 0;

        /// <inheritdoc />
        public bool IsInitialized { get; private set; }

        /// <inheritdoc />
        public PlayerData Current { get; private set; } = new PlayerData();

        /// <inheritdoc />
        public event Action<PlayerData> Loaded;

        /// <inheritdoc />
        public event Action<PlayerData> Saved;

        /// <inheritdoc />
        public event Action<PlayerData> Changed;

        /// <inheritdoc />
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            ServiceLocator.TryGet(out configProvider);
            string fileName = configProvider?.Game != null ? configProvider.Game.SaveFileName : "moverush.save.json";
            repository = new JsonSaveRepository(fileName);

            ServiceLocator.Register<ISaveService>(this);
            Load();
            StartAutoSave();

            IsInitialized = true;
            Log.Info($"SaveManager initialised. Profile: {repository.Location}", this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (autoSaveRoutine != null)
            {
                StopCoroutine(autoSaveRoutine);
                autoSaveRoutine = null;
            }

            Save();

            Loaded = null;
            Saved = null;
            Changed = null;

            ServiceLocator.Unregister<ISaveService>();
            IsInitialized = false;
        }

        /// <inheritdoc />
        public bool Load()
        {
            bool restored = false;

            if (repository.TryRead(out string payload))
            {
                try
                {
                    SaveFile file = JsonUtility.FromJson<SaveFile>(payload);
                    if (file?.Player != null)
                    {
                        Migrate(file);
                        Current = file.Player;
                        restored = true;
                    }
                }
                catch (Exception exception)
                {
                    Log.Exception(exception, "SaveManager: the stored profile is corrupt, defaults will be used.", this);
                }
            }

            if (!restored)
            {
                Current = CreateDefaultProfile();
                Log.Info("SaveManager: a new profile was created.", this);
            }

            isDirty = !restored;
            Loaded?.Invoke(Current);
            return restored;
        }

        /// <inheritdoc />
        public bool Save()
        {
            try
            {
                bool pretty = configProvider?.Game == null || configProvider.Game.PrettyPrintSave;
                string payload = JsonUtility.ToJson(SaveFile.Wrap(Current), pretty);

                if (!repository.Write(payload))
                {
                    return false;
                }

                isDirty = false;
                Saved?.Invoke(Current);
                return true;
            }
            catch (Exception exception)
            {
                Log.Exception(exception, "SaveManager: serialising the profile failed.", this);
                return false;
            }
        }

        /// <inheritdoc />
        public void Modify(Action<PlayerData> mutation, bool saveImmediately = false)
        {
            if (mutation == null)
            {
                return;
            }

            mutation.Invoke(Current);
            isDirty = true;
            Changed?.Invoke(Current);

            if (saveImmediately)
            {
                Save();
            }
        }

        /// <inheritdoc />
        public void DeleteSave()
        {
            repository.Delete();
            Current = CreateDefaultProfile();
            isDirty = true;

            Loaded?.Invoke(Current);
            Changed?.Invoke(Current);
            Log.Warning("SaveManager: the stored profile was deleted and reset to defaults.", this);
        }

        /// <summary>Builds a profile from the configuration assets.</summary>
        /// <returns>A default profile.</returns>
        private PlayerData CreateDefaultProfile()
        {
            return PlayerData.CreateDefault(
                configProvider?.Player,
                configProvider?.Audio,
                configProvider?.Game != null ? configProvider.Game.DefaultQualityLevel : 0,
                configProvider?.Game != null ? configProvider.Game.DefaultLanguageCode : "en");
        }

        /// <summary>
        /// Upgrades a profile written by an older build. Each future schema change adds one
        /// step here so returning players keep their progress.
        /// </summary>
        /// <param name="file">Envelope that was just read.</param>
        private static void Migrate(SaveFile file)
        {
            if (file.Version == SaveFile.CurrentVersion)
            {
                return;
            }

            Log.Info($"SaveManager: migrating profile from version {file.Version} to {SaveFile.CurrentVersion}.");
            file.Version = SaveFile.CurrentVersion;
        }

        /// <summary>Starts the periodic auto-save timer when configuration enables it.</summary>
        private void StartAutoSave()
        {
            float interval = configProvider?.Game != null ? configProvider.Game.AutoSaveInterval : 0f;
            if (interval <= 0f)
            {
                return;
            }

            autoSaveRoutine = StartCoroutine(AutoSaveRoutine(interval));
        }

        /// <summary>Writes the profile at a fixed interval, but only when it has changed.</summary>
        /// <param name="interval">Seconds between checks.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator AutoSaveRoutine(float interval)
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(interval);

            while (true)
            {
                yield return wait;

                if (isDirty)
                {
                    Save();
                }
            }
        }

        /// <summary>Flushes pending changes when the application is suspended.</summary>
        /// <param name="pauseStatus">True when the application is going into the background.</param>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && IsInitialized && isDirty)
            {
                Save();
            }
        }
    }
}
