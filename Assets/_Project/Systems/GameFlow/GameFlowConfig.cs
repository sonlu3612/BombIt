using System;
using System.Collections.Generic;
using _Project.Gameplay.AI.Scripts;
using UnityEngine;

namespace _Project.Systems.GameFlow
{
    public enum ArenaOption
    {
        Beach = 0,
        Bakery = 1,
        Forest = 2,
        Random = 99
    }

    public enum AiDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    public enum CharacterId
    {
        Character1 = 0,
        Character2 = 1,
        Character3 = 2,
        Character4 = 3
    }

    [Serializable]
    public class ArenaDefinition
    {
        public ArenaOption arena;
        public string displayName;
        public string sceneName;
        public bool isSelectable = true;
    }

    [Serializable]
    public class CharacterDefinition
    {
        public CharacterId characterId;
        public string displayName;
        public RuntimeAnimatorController animatorController;
        public Sprite portraitSprite;
    }

    [CreateAssetMenu(
        fileName = "GameFlowConfig",
        menuName = "BombIt/Game Flow Config")]
    public class GameFlowConfig : ScriptableObject
    {
        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameSettingScene = "GameSetting";
        [SerializeField] private string singlePlayerCharacterScene = "Characters 1";
        [SerializeField] private string twoPlayerCharacterScene = "Characters2";

        [Header("Gameplay Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject botPrefab;

        [Header("Arenas")]
        [SerializeField] private ArenaDefinition[] arenas;

        [Header("Characters")]
        [SerializeField] private CharacterDefinition[] characters;

        [Header("AI Difficulty")]
        [SerializeField] private BotConfig easyBotConfig = new()
        {
            thinkInterval = 0.2f,
            repathInterval = 0.22f,
            findRange = 5,
            avoidDangerCells = true,
            reachThreshold = 0.06f,
            wanderMaxSearchCells = 16,
            wanderMinPathLength = 3,
            idleChance = 0.18f,
            idleDurationRange = new Vector2(0.25f, 0.6f),
            itemChance = 0.4f,
            attackChance = 0.35f,
            breakBlockChance = 0.5f,
            plantBombChance = 0.55f,
            bombCooldown = 0.6f,
            attackSearchRange = 5,
            escapeSearchRange = 6
        };

        [SerializeField] private BotConfig normalBotConfig = new();

        [SerializeField] private BotConfig hardBotConfig = new()
        {
            thinkInterval = 0.08f,
            repathInterval = 0.1f,
            findRange = 10,
            avoidDangerCells = true,
            reachThreshold = 0.04f,
            wanderMaxSearchCells = 28,
            wanderMinPathLength = 5,
            idleChance = 0.03f,
            idleDurationRange = new Vector2(0.08f, 0.25f),
            itemChance = 0.95f,
            attackChance = 0.92f,
            breakBlockChance = 0.96f,
            plantBombChance = 0.98f,
            bombCooldown = 0.2f,
            attackSearchRange = 10,
            escapeSearchRange = 10
        };

        public string MainMenuScene => mainMenuScene;
        public string GameSettingScene => gameSettingScene;
        public string SinglePlayerCharacterScene => singlePlayerCharacterScene;
        public string TwoPlayerCharacterScene => twoPlayerCharacterScene;
        public GameObject PlayerPrefab => playerPrefab;
        public GameObject BotPrefab => botPrefab;

        public int CharacterCount => characters != null ? characters.Length : 0;

        public ArenaOption ResolveArena(ArenaOption requestedArena)
        {
            if (requestedArena != ArenaOption.Random)
                return requestedArena;

            List<ArenaDefinition> selectableArenas = GetSelectableArenas();
            if (selectableArenas.Count == 0)
                return ArenaOption.Bakery;

            int index = UnityEngine.Random.Range(0, selectableArenas.Count);
            return selectableArenas[index].arena;
        }

        public string GetSceneForArena(ArenaOption arena)
        {
            ArenaDefinition definition = GetArenaDefinition(arena);
            return definition != null ? definition.sceneName : string.Empty;
        }

        public ArenaDefinition GetArenaDefinition(ArenaOption arena)
        {
            if (arenas == null)
                return null;

            foreach (ArenaDefinition definition in arenas)
            {
                if (definition != null && definition.arena == arena)
                    return definition;
            }

            return null;
        }

        public List<ArenaDefinition> GetSelectableArenas()
        {
            List<ArenaDefinition> results = new();

            if (arenas == null)
                return results;

            foreach (ArenaDefinition definition in arenas)
            {
                if (definition == null || !definition.isSelectable || string.IsNullOrWhiteSpace(definition.sceneName))
                    continue;

                results.Add(definition);
            }

            return results;
        }

        public CharacterDefinition GetCharacter(CharacterId characterId)
        {
            if (characters == null)
                return null;

            foreach (CharacterDefinition definition in characters)
            {
                if (definition != null && definition.characterId == characterId)
                    return definition;
            }

            return null;
        }

        public CharacterId GetCharacterIdAt(int index)
        {
            if (characters == null || characters.Length == 0)
                return CharacterId.Character1;

            index = Mathf.Clamp(index, 0, characters.Length - 1);
            return characters[index].characterId;
        }

        public BotConfig GetBotConfig(AiDifficulty difficulty)
        {
            return difficulty switch
            {
                AiDifficulty.Easy => easyBotConfig.Clone(),
                AiDifficulty.Hard => hardBotConfig.Clone(),
                _ => normalBotConfig.Clone()
            };
        }

        public int ClampEnemyCount(int playerCount, int requestedEnemyCount)
        {
            int minEnemies = playerCount <= 1 ? 1 : 0;
            int maxEnemies = playerCount <= 1 ? 3 : 2;
            return Mathf.Clamp(requestedEnemyCount, minEnemies, maxEnemies);
        }
    }
}
