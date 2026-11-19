namespace MathQuest.Data
{
    public enum PlayerCharacter
    {
        Boy,
        Girl
    }

    /// <summary>
    /// What the player picked on the character and name screens. Lives only in memory for now; it will be
    /// saved once saving exists.
    /// </summary>
    public static class PlayerProfile
    {
        public static PlayerCharacter Character { get; set; } = PlayerCharacter.Boy;
        public static string PlayerName { get; set; } = string.Empty;
    }
}
