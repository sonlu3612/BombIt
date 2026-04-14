#if UNITY_EDITOR
using System.IO;
using _Project.Systems.GameFlow;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor
{
    public static class GameFlowConfigCreator
    {
        private const string AssetPath = "Assets/_Project/Data/Configs/DefaultGameFlowConfig.asset";

        [MenuItem("Tools/BombIt/Create Or Update Default Game Flow Config")]
        public static void CreateOrUpdate()
        {
            GameFlowConfig config = AssetDatabase.LoadAssetAtPath<GameFlowConfig>(AssetPath);
            bool isNewAsset = config == null;

            if (isNewAsset)
            {
                string directory = Path.GetDirectoryName(AssetPath);
                if (!string.IsNullOrWhiteSpace(directory) && !AssetDatabase.IsValidFolder(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                config = ScriptableObject.CreateInstance<GameFlowConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
            }

            SerializedObject serializedObject = new(config);

            serializedObject.FindProperty("mainMenuScene").stringValue = "MainMenu";
            serializedObject.FindProperty("gameSettingScene").stringValue = "GameSetting";
            serializedObject.FindProperty("singlePlayerCharacterScene").stringValue = "Characters 1";
            serializedObject.FindProperty("twoPlayerCharacterScene").stringValue = "Characters2";

            serializedObject.FindProperty("playerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Gameplay/Player/Prefabs/Player.prefab");
            serializedObject.FindProperty("botPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Gameplay/AI/Prefabs/Bot.prefab");

            SerializedProperty arenasProperty = serializedObject.FindProperty("arenas");
            arenasProperty.arraySize = 3;
            SetArena(arenasProperty, 0, ArenaOption.Beach, "Beach", "Map", false);
            SetArena(arenasProperty, 1, ArenaOption.Bakery, "Bakery", "Map3", true);
            SetArena(arenasProperty, 2, ArenaOption.Forest, "Forest", "Map2", false);

            SerializedProperty charactersProperty = serializedObject.FindProperty("characters");
            charactersProperty.arraySize = 4;
            SetCharacter(
                charactersProperty,
                0,
                CharacterId.Character1,
                "Character 1",
                "Assets/_Project/Gameplay/Player/Animations/PlayerAnimator.controller");
            SetCharacter(
                charactersProperty,
                1,
                CharacterId.Character2,
                "Character 2",
                "Assets/_Project/Gameplay/Player/Animations/PlayerAnimation2.overrideController");
            SetCharacter(
                charactersProperty,
                2,
                CharacterId.Character3,
                "Character 3",
                "Assets/_Project/Gameplay/Player/Animations/PlayerAnimation3.overrideController");
            SetCharacter(
                charactersProperty,
                3,
                CharacterId.Character4,
                "Character 4",
                "Assets/_Project/Gameplay/Player/Animations/PlayerAnimation4.overrideController");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = config;
            Debug.Log(isNewAsset
                ? $"Created default GameFlowConfig at {AssetPath}"
                : $"Updated default GameFlowConfig at {AssetPath}");
        }

        private static void SetArena(
            SerializedProperty arenasProperty,
            int index,
            ArenaOption arenaOption,
            string displayName,
            string sceneName,
            bool isSelectable)
        {
            SerializedProperty element = arenasProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("arena").enumValueIndex = (int)arenaOption;
            element.FindPropertyRelative("displayName").stringValue = displayName;
            element.FindPropertyRelative("sceneName").stringValue = sceneName;
            element.FindPropertyRelative("isSelectable").boolValue = isSelectable;
        }

        private static void SetCharacter(
            SerializedProperty charactersProperty,
            int index,
            CharacterId characterId,
            string displayName,
            string controllerPath)
        {
            SerializedProperty element = charactersProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("characterId").enumValueIndex = (int)characterId;
            element.FindPropertyRelative("displayName").stringValue = displayName;
            element.FindPropertyRelative("animatorController").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        }
    }
}
#endif
