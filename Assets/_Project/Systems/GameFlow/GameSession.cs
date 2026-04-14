using System.Collections.Generic;

namespace _Project.Systems.GameFlow
{
    public static class GameSession
    {
        private static readonly CharacterId?[] selectedCharacters = new CharacterId?[2];

        public static bool IsConfigured { get; private set; }
        public static int PlayerCount { get; private set; } = 1;
        public static int EnemyCount { get; private set; } = 3;
        public static int TotalRounds { get; private set; } = 5;
        public static int CurrentRound { get; private set; } = 1;
        public static ArenaOption SelectedArena { get; private set; } = ArenaOption.Bakery;
        public static AiDifficulty Difficulty { get; private set; } = AiDifficulty.Normal;

        public static void StartNewSession(
            int playerCount,
            int enemyCount,
            int totalRounds,
            ArenaOption selectedArena,
            AiDifficulty difficulty)
        {
            IsConfigured = true;
            PlayerCount = playerCount;
            EnemyCount = enemyCount;
            TotalRounds = totalRounds;
            CurrentRound = 1;
            SelectedArena = selectedArena;
            Difficulty = difficulty;
            ClearCharacterSelections();
        }

        public static void ClearSession()
        {
            IsConfigured = false;
            PlayerCount = 1;
            EnemyCount = 3;
            TotalRounds = 5;
            CurrentRound = 1;
            SelectedArena = ArenaOption.Bakery;
            Difficulty = AiDifficulty.Normal;
            ClearCharacterSelections();
        }

        public static void ClearCharacterSelections()
        {
            for (int i = 0; i < selectedCharacters.Length; i++)
                selectedCharacters[i] = null;
        }

        public static void SetSelectedCharacter(int playerIndex, CharacterId characterId)
        {
            if (playerIndex < 0 || playerIndex >= selectedCharacters.Length)
                return;

            selectedCharacters[playerIndex] = characterId;
        }

        public static CharacterId? GetSelectedCharacter(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= selectedCharacters.Length)
                return null;

            return selectedCharacters[playerIndex];
        }

        public static bool HasAllHumanCharactersSelected()
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                if (!selectedCharacters[i].HasValue)
                    return false;
            }

            return true;
        }

        public static List<CharacterId> GetRemainingCharacters(GameFlowConfig config)
        {
            List<CharacterId> remaining = new();

            if (config == null)
                return remaining;

            for (int i = 0; i < config.CharacterCount; i++)
            {
                CharacterId characterId = config.GetCharacterIdAt(i);
                bool alreadyTaken = false;

                for (int playerIndex = 0; playerIndex < PlayerCount; playerIndex++)
                {
                    if (selectedCharacters[playerIndex] == characterId)
                    {
                        alreadyTaken = true;
                        break;
                    }
                }

                if (!alreadyTaken)
                    remaining.Add(characterId);
            }

            return remaining;
        }

        public static bool AdvanceRound()
        {
            if (CurrentRound >= TotalRounds)
                return false;

            CurrentRound++;
            return true;
        }
    }
}
