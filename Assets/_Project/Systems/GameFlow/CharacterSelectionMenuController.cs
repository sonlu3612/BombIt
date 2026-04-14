using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Systems.GameFlow
{
    public class CharacterSelectionMenuController : MonoBehaviour
    {
        [Header("Shared Config")]
        [SerializeField] private GameFlowConfig gameFlowConfig;

        [Header("Scene Mode")]
        [SerializeField] private bool twoPlayerMode;

        [Header("Optional UI State")]
        [SerializeField] private Button[] characterButtons;
        [SerializeField] private CharacterSelectionButtonVisual[] characterButtonVisuals;
        [SerializeField] private Button startButton;
        [SerializeField] private Image[] playerPreviewImages;
        [SerializeField] private bool debugPreviewLogs = true;

        private int editingPlayerIndex;

        private void Start()
        {
            if (!GameSession.IsConfigured)
            {
                GoBack();
                return;
            }

            twoPlayerMode = GameSession.PlayerCount == 2;
            editingPlayerIndex = GameSession.PlayerCount == 2 ? 0 : 0;

            if (debugPreviewLogs)
            {
                int previewCount = playerPreviewImages != null ? playerPreviewImages.Length : 0;
                Debug.Log($"[{nameof(CharacterSelectionMenuController)}] Start on scene '{SceneManager.GetActiveScene().name}'. PlayerCount={GameSession.PlayerCount}, PreviewImageCount={previewCount}", this);

                if (playerPreviewImages != null)
                {
                    for (int i = 0; i < playerPreviewImages.Length; i++)
                    {
                        Image preview = playerPreviewImages[i];
                        Debug.Log(
                            $"[{nameof(CharacterSelectionMenuController)}] Preview slot {i}: image={(preview != null ? preview.name : "NULL")}, active={(preview != null && preview.gameObject.activeInHierarchy)}",
                            this);
                    }
                }
            }

            RefreshUiState();
        }

        public void SelectCharacter(int characterIndex)
        {
            if (gameFlowConfig == null)
            {
                Debug.LogError($"[{nameof(CharacterSelectionMenuController)}] Missing GameFlowConfig.", this);
                return;
            }

            CharacterId selectedCharacter = gameFlowConfig.GetCharacterIdAt(characterIndex);

            if (debugPreviewLogs)
                Debug.Log($"[{nameof(CharacterSelectionMenuController)}] SelectCharacter index={characterIndex}, resolved={selectedCharacter}, editingPlayerIndex={editingPlayerIndex}, twoPlayerMode={twoPlayerMode}", this);

            if (!twoPlayerMode)
            {
                GameSession.SetSelectedCharacter(0, selectedCharacter);
                RefreshUiState();
                return;
            }

            int otherPlayerIndex = editingPlayerIndex == 0 ? 1 : 0;
            CharacterId? otherCharacter = GameSession.GetSelectedCharacter(otherPlayerIndex);

            if (otherCharacter.HasValue && otherCharacter.Value == selectedCharacter)
                return;

            GameSession.SetSelectedCharacter(editingPlayerIndex, selectedCharacter);
            editingPlayerIndex = otherPlayerIndex;
            RefreshUiState();
        }

        public void FocusPlayer(int playerIndex)
        {
            editingPlayerIndex = Mathf.Clamp(playerIndex, 0, 1);
            RefreshUiState();
        }

        public void StartGame()
        {
            if (gameFlowConfig == null)
            {
                Debug.LogError($"[{nameof(CharacterSelectionMenuController)}] Missing GameFlowConfig.", this);
                return;
            }

            if (!GameSession.HasAllHumanCharactersSelected())
            {
                Debug.LogWarning($"[{nameof(CharacterSelectionMenuController)}] Character selection is incomplete.", this);
                return;
            }

            string sceneName = gameFlowConfig.GetSceneForArena(GameSession.SelectedArena);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError($"[{nameof(CharacterSelectionMenuController)}] Arena scene is not configured for {GameSession.SelectedArena}.", this);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        public void GoBack()
        {
            string targetScene = gameFlowConfig != null && !string.IsNullOrWhiteSpace(gameFlowConfig.GameSettingScene)
                ? gameFlowConfig.GameSettingScene
                : "GameSetting";

            SceneManager.LoadScene(targetScene);
        }

        private void RefreshUiState()
        {
            if (startButton != null)
                startButton.interactable = GameSession.HasAllHumanCharactersSelected();

            CharacterId? playerOneCharacter = GameSession.GetSelectedCharacter(0);
            CharacterId? playerTwoCharacter = GameSession.GetSelectedCharacter(1);

            RefreshPreviewImage(0, playerOneCharacter);
            RefreshPreviewImage(1, playerTwoCharacter);

            int buttonCount = characterButtons != null ? characterButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                Button button = characterButtons[i];
                if (button == null || gameFlowConfig == null)
                    continue;

                CharacterId representedCharacter = gameFlowConfig.GetCharacterIdAt(i);
                bool selectedByOtherPlayer = twoPlayerMode &&
                    ((editingPlayerIndex == 0 && playerTwoCharacter == representedCharacter) ||
                     (editingPlayerIndex == 1 && playerOneCharacter == representedCharacter));

                bool selectedByCurrentPlayer = playerOneCharacter == representedCharacter ||
                    (twoPlayerMode && playerTwoCharacter == representedCharacter);

                button.interactable = !selectedByOtherPlayer;

                if (characterButtonVisuals != null && i < characterButtonVisuals.Length && characterButtonVisuals[i] != null)
                    characterButtonVisuals[i].SetState(selectedByCurrentPlayer, selectedByOtherPlayer);
            }
        }

        private void RefreshPreviewImage(int playerIndex, CharacterId? characterId)
        {
            if (playerPreviewImages == null || playerIndex < 0 || playerIndex >= playerPreviewImages.Length)
            {
                if (debugPreviewLogs)
                    Debug.LogWarning($"[{nameof(CharacterSelectionMenuController)}] Preview slot {playerIndex} is out of range. PreviewImageCount={(playerPreviewImages != null ? playerPreviewImages.Length : 0)}", this);
                return;
            }

            Image previewImage = playerPreviewImages[playerIndex];
            if (previewImage == null)
            {
                if (debugPreviewLogs)
                    Debug.LogWarning($"[{nameof(CharacterSelectionMenuController)}] Preview image at slot {playerIndex} is NULL.", this);
                return;
            }

            if (!characterId.HasValue || gameFlowConfig == null)
            {
                previewImage.enabled = false;

                if (debugPreviewLogs)
                    Debug.Log($"[{nameof(CharacterSelectionMenuController)}] Preview slot {playerIndex} cleared. Character selection is empty or config missing.", this);

                return;
            }

            CharacterDefinition character = gameFlowConfig.GetCharacter(characterId.Value);
            if (character == null || character.portraitSprite == null)
            {
                previewImage.enabled = false;

                if (debugPreviewLogs)
                    Debug.LogWarning($"[{nameof(CharacterSelectionMenuController)}] Preview slot {playerIndex} missing portrait. Character={characterId}, CharacterExists={(character != null)}", this);

                return;
            }

            previewImage.sprite = character.portraitSprite;
            previewImage.preserveAspect = true;
            previewImage.enabled = true;

            if (debugPreviewLogs)
                Debug.Log($"[{nameof(CharacterSelectionMenuController)}] Preview slot {playerIndex} updated. Character={characterId}, Sprite={character.portraitSprite.name}, Image={previewImage.name}", previewImage);
        }
    }
}
