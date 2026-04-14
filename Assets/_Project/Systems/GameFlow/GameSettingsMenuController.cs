using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Systems.GameFlow
{
    public class GameSettingsMenuController : MonoBehaviour
    {
        [System.Serializable]
        private class ButtonVisualState
        {
            public Button button = null;
            public Graphic selectedGraphic = null;
            public GameObject selectedObject = null;
            public Sprite normalSprite;
            public Sprite selectedSprite;
        }

        [Header("Shared Config")]
        [SerializeField] private GameFlowConfig gameFlowConfig;

        [Header("Optional UI State")]
        [SerializeField] private ButtonVisualState[] playerButtons;
        [SerializeField] private ButtonVisualState[] enemyButtons;
        [SerializeField] private ButtonVisualState[] levelButtons;
        [SerializeField] private ButtonVisualState[] arenaButtons;
        [SerializeField] private ButtonVisualState[] difficultyButtons;
        [SerializeField] private Button okButton;

        [Header("Selected Visual")]
        [SerializeField] private bool disableSelectedButton = false;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new(1f, 0.95f, 0.7f, 1f);

        private int playerCount = 1;
        private int enemyCount = 3;
        private int levelCount = 5;
        private ArenaOption arenaOption = ArenaOption.Bakery;
        private AiDifficulty difficulty = AiDifficulty.Normal;

        private void Start()
        {
            if (gameFlowConfig != null)
                enemyCount = gameFlowConfig.ClampEnemyCount(playerCount, enemyCount);

            CacheButtonVisualDefaults(playerButtons);
            CacheButtonVisualDefaults(enemyButtons);
            CacheButtonVisualDefaults(levelButtons);
            CacheButtonVisualDefaults(arenaButtons);
            CacheButtonVisualDefaults(difficultyButtons);

            RefreshUiState();
        }

        public void SelectPlayerCount(int value)
        {
            playerCount = Mathf.Clamp(value, 1, 2);

            if (gameFlowConfig != null)
                enemyCount = gameFlowConfig.ClampEnemyCount(playerCount, enemyCount);

            RefreshUiState();
        }

        public void SelectEnemyCount(int value)
        {
            enemyCount = gameFlowConfig != null
                ? gameFlowConfig.ClampEnemyCount(playerCount, value)
                : value;

            RefreshUiState();
        }

        public void SelectLevelCount(int value)
        {
            levelCount = Mathf.Max(1, value);
            RefreshUiState();
        }

        public void SelectArena(int value)
        {
            arenaOption = (ArenaOption)value;
            RefreshUiState();
        }

        public void SelectDifficulty(int value)
        {
            difficulty = (AiDifficulty)value;
            RefreshUiState();
        }

        public void ConfirmSelection()
        {
            if (gameFlowConfig == null)
            {
                Debug.LogError($"[{nameof(GameSettingsMenuController)}] Missing GameFlowConfig.", this);
                return;
            }

            ArenaOption resolvedArena = gameFlowConfig.ResolveArena(arenaOption);
            GameSession.StartNewSession(
                playerCount,
                gameFlowConfig.ClampEnemyCount(playerCount, enemyCount),
                levelCount,
                resolvedArena,
                difficulty);

            string nextScene = playerCount == 1
                ? gameFlowConfig.SinglePlayerCharacterScene
                : gameFlowConfig.TwoPlayerCharacterScene;

            if (string.IsNullOrWhiteSpace(nextScene))
            {
                Debug.LogError($"[{nameof(GameSettingsMenuController)}] Character select scene is not configured.", this);
                return;
            }

            SceneManager.LoadScene(nextScene);
        }

        public void GoBack()
        {
            string targetScene = gameFlowConfig != null && !string.IsNullOrWhiteSpace(gameFlowConfig.MainMenuScene)
                ? gameFlowConfig.MainMenuScene
                : "MainMenu";

            SceneManager.LoadScene(targetScene);
        }

        private void RefreshUiState()
        {
            SetSelectedButton(playerButtons, playerCount == 1 ? 0 : 1);
            SetSelectedButton(enemyButtons, GetEnemyButtonIndex(enemyCount));
            SetSelectedButton(levelButtons, GetLevelButtonIndex(levelCount));
            SetSelectedButton(arenaButtons, GetArenaButtonIndex(arenaOption));
            SetSelectedButton(difficultyButtons, (int)difficulty);

            if (enemyButtons != null)
            {
                for (int i = 0; i < enemyButtons.Length; i++)
                {
                    ButtonVisualState visualState = enemyButtons[i];
                    if (visualState == null || visualState.button == null)
                        continue;

                    int representedEnemyCount = 3 - i;
                    bool valid = gameFlowConfig == null
                        || representedEnemyCount == gameFlowConfig.ClampEnemyCount(playerCount, representedEnemyCount);

                    visualState.button.gameObject.SetActive(valid);
                }
            }

            if (okButton != null)
                okButton.interactable = true;
        }

        private void SetSelectedButton(ButtonVisualState[] buttons, int selectedIndex)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                ButtonVisualState visualState = buttons[i];
                if (visualState == null || visualState.button == null)
                    continue;

                bool isSelected = i == selectedIndex;
                visualState.button.interactable = !disableSelectedButton || !isSelected;

                Graphic graphic = visualState.selectedGraphic != null
                    ? visualState.selectedGraphic
                    : visualState.button.targetGraphic;

                if (graphic != null)
                {
                    graphic.color = isSelected ? selectedColor : normalColor;

                    if (graphic is Image image)
                    {
                        Sprite resolvedNormalSprite = visualState.normalSprite != null
                            ? visualState.normalSprite
                            : image.sprite;

                        Sprite resolvedSelectedSprite = visualState.selectedSprite != null
                            ? visualState.selectedSprite
                            : visualState.button.spriteState.selectedSprite;

                        if (isSelected && resolvedSelectedSprite != null)
                            image.sprite = resolvedSelectedSprite;
                        else if (!isSelected && resolvedNormalSprite != null)
                            image.sprite = resolvedNormalSprite;
                    }
                }

                if (visualState.selectedObject != null)
                    visualState.selectedObject.SetActive(isSelected);
            }
        }

        private static void CacheButtonVisualDefaults(ButtonVisualState[] buttons)
        {
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                ButtonVisualState visualState = buttons[i];
                if (visualState == null || visualState.button == null)
                    continue;

                Graphic graphic = visualState.selectedGraphic != null
                    ? visualState.selectedGraphic
                    : visualState.button.targetGraphic;

                if (graphic is Image image)
                {
                    if (visualState.normalSprite == null)
                        visualState.normalSprite = image.sprite;

                    if (visualState.selectedSprite == null)
                        visualState.selectedSprite = visualState.button.spriteState.selectedSprite;
                }
            }
        }

        private static int GetLevelCountFromIndex(int index)
        {
            return index switch
            {
                0 => 5,
                1 => 10,
                2 => 15,
                3 => 20,
                _ => 5
            };
        }

        private static ArenaOption GetArenaOptionFromIndex(int index)
        {
            return index switch
            {
                0 => ArenaOption.Beach,
                1 => ArenaOption.Bakery,
                2 => ArenaOption.Forest,
                3 => ArenaOption.Random,
                _ => ArenaOption.Bakery
            };
        }

        private static int GetEnemyButtonIndex(int selectedEnemyCount)
        {
            return selectedEnemyCount switch
            {
                3 => 0,
                2 => 1,
                1 => 2,
                0 => 3,
                _ => 0
            };
        }

        private static int GetLevelButtonIndex(int selectedLevelCount)
        {
            return selectedLevelCount switch
            {
                5 => 0,
                10 => 1,
                15 => 2,
                20 => 3,
                _ => 0
            };
        }

        private static int GetArenaButtonIndex(ArenaOption selectedArena)
        {
            return selectedArena switch
            {
                ArenaOption.Beach => 0,
                ArenaOption.Bakery => 1,
                ArenaOption.Forest => 2,
                ArenaOption.Random => 3,
                _ => 1
            };
        }
    }
}
