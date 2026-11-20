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
