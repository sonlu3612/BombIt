using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.Systems.GameFlow
{
    [RequireComponent(typeof(Button))]
    public class CharacterSelectionButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite blockedSprite;

        private bool isPointerOver;
        private bool isSelected;
        private bool isBlocked;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (targetImage == null && button != null)
                targetImage = button.targetGraphic as Image;

            CacheSpritesFromButtonState();

            if (button != null)
                button.transition = Selectable.Transition.None;

            ApplyVisual();
        }

        public void SetState(bool selected, bool blocked)
        {
            isSelected = selected;
            isBlocked = blocked;

            if (button != null)
                button.interactable = !blocked;

            ApplyVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            ApplyVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (targetImage == null)
                return;

            Sprite resolvedSprite = ResolveSprite();
            if (resolvedSprite != null)
                targetImage.sprite = resolvedSprite;
        }

        private Sprite ResolveSprite()
        {
            Sprite resolvedBlocked = blockedSprite != null ? blockedSprite : normalSprite;
            Sprite resolvedHover = hoverSprite != null ? hoverSprite : normalSprite;
            Sprite resolvedSelected = selectedSprite != null ? selectedSprite : resolvedHover;

            if (isSelected)
                return resolvedSelected;

            if (isBlocked)
                return resolvedBlocked;

            if (isPointerOver)
                return resolvedHover;

            return normalSprite;
        }

        private void CacheSpritesFromButtonState()
        {
            if (targetImage != null && normalSprite == null)
                normalSprite = targetImage.sprite;

            if (button == null)
                return;

            SpriteState spriteState = button.spriteState;

            if (hoverSprite == null)
                hoverSprite = spriteState.highlightedSprite;

            if (selectedSprite == null)
                selectedSprite = spriteState.selectedSprite != null
                    ? spriteState.selectedSprite
                    : hoverSprite;

            if (blockedSprite == null)
                blockedSprite = spriteState.disabledSprite;
        }
    }
}
