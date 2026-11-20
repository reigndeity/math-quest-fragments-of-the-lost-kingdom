using System;
using System.IO;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// Everything the game remembers between sessions. Saved as one JSON file, so new things
    /// (level progress, stars, best times) are just new fields here.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;

        /// <summary>False until the player has picked a character and a name.</summary>
        public bool hasSave;

        public string playerName = string.Empty;
        public PlayerCharacter character = PlayerCharacter.Boy;

        /// <summary>One entry for every level the player has won at least once.</summary>
        public System.Collections.Generic.List<LevelRecord> levels = new();
    }

    /// <summary>What the player achieved on one level. A level has a record once it has been won.</summary>
    [Serializable]
    public sealed class LevelRecord
    {
        /// <summary>The LevelResource's asset name, for example LevelResource_Act1_3.</summary>
        public string levelId;

        /// <summary>The most stars earned (1 to 3).</summary>
        public int stars;

        /// <summary>The fastest answering time of a win, in seconds.</summary>
        public float bestTime;
    }

    /// <summary>Formats seconds the way the game shows times: 02:05.</summary>
    public static class TimeFormat
    {
        public static string Minutes(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }

    /// <summary>
    /// Loads, saves and resets the save file. The file lives in Application.persistentDataPath, which works on
    /// Android without any permission and is kept when the game updates.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "savegame.json";

        private static SaveData data;

        /// <summary>Raised after the save is created or reset, so open screens can refresh themselves.</summary>
        public static event Action Changed;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>The current save. Loaded from disk the first time it is asked for.</summary>
        public static SaveData Data
        {
            get
            {
                if (data == null)
                    Load();
                return data;
            }
        }

        /// <summary>True when there is a saved game to continue.</summary>
        public static bool HasSave => Data.hasSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Needed when the editor does not reload the domain between Play sessions.
            data = null;
            Changed = null;
        }

        /// <summary>Reads the save file. A missing or damaged file just means there is no save yet.</summary>
        public static void Load()
        {
            data = new SaveData();

            try
            {
                if (File.Exists(FilePath))
                {
                    SaveData loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                    if (loaded != null)
                        data = loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not read the save file, starting without one. {e.Message}");
                data = new SaveData();
            }

            PlayerProfile.Character = data.character;
            PlayerProfile.PlayerName = data.playerName ?? string.Empty;
        }

        /// <summary>Called once the player has picked a character and typed a name: this is the first save.</summary>
        public static void StartNewGame(PlayerCharacter character, string playerName)
        {
            data = new SaveData
            {
                hasSave = true,
                character = character,
                playerName = playerName ?? string.Empty
            };

            PlayerProfile.Character = data.character;
            PlayerProfile.PlayerName = data.playerName;

            Save();
            Changed?.Invoke();
        }

        /// <summary>The record of a level, or null if it has not been won yet.</summary>
        public static LevelRecord GetLevel(LevelResource level)
        {
            if (level == null)
                return null;

            foreach (LevelRecord record in Data.levels)
            {
                if (record.levelId == level.name)
                    return record;
            }

            return null;
        }

        /// <summary>
        /// Saves a win. A better result replaces the old one, and a worse one never does: stars only go up and the
        /// best time only goes down. Winning a level is what opens the next one.
        /// </summary>
        public static void RecordWin(LevelResource level, int stars, float answeringTime)
        {
            if (level == null)
                return;

            LevelRecord record = GetLevel(level);
            if (record == null)
            {
                Data.levels.Add(new LevelRecord { levelId = level.name, stars = stars, bestTime = answeringTime });
            }
            else
            {
                record.stars = Mathf.Max(record.stars, stars);
                if (record.bestTime <= 0f || answeringTime < record.bestTime)
                    record.bestTime = answeringTime;
            }

            Save();
            Changed?.Invoke();
        }

        /// <summary>Writes the current data to disk.</summary>
        public static void Save()
        {
            try
            {
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));

                // Write to a temporary file first, so a crash half way never leaves a broken save.
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not save the game. {e.Message}");
            }
        }

        /// <summary>Erases the save and everything remembered from it. Level progress will be cleared here later too.</summary>
        public static void Reset()
        {
            data = new SaveData();

            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not delete the save file. {e.Message}");
            }

            PlayerProfile.Character = PlayerCharacter.Boy;
            PlayerProfile.PlayerName = string.Empty;
            LevelSession.Clear();
            LevelSession.ForgetQuestions();

            Changed?.Invoke();
        }
    }
}
