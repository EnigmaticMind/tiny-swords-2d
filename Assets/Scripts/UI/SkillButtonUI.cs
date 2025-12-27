using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TinySwords2D.Data;
using TinySwords2D.Gameplay;
using System.Collections.Generic;
using UnityEngine.InputSystem;


namespace TinySwords2D.UI
{
    // Remove RequireComponent since Button is on child objects now
    public class SkillButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI References (Auto-found if not assigned)")]
        [SerializeField] public GameObject unlockedFrame;
        [SerializeField] public GameObject lockedFrame;
        [SerializeField] public Image iconImage;

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPrefab; // The prefab to instantiate
        [SerializeField] private Transform tooltipParent; // Canvas or parent to instantiate under

        private SkillTooltip currentTooltipInstance; // Track current tooltip

        private Skill boundSkill;
        public int? hotkeyNumber = null;

        private bool isLocked = false;

        public bool isPressed = false;
        public bool isHovered = false;
        private bool visualState = false;

        private Color selectedColor = new Color(72 / 255f, 72 / 255f, 72 / 255f, 1f);
        private Color defaultColor = new Color(1f, 1f, 1f, 1f);

        public Skill BoundSkill => boundSkill;

        public UnityEvent<SkillButtonUI> OnSkillInvoked = new();

        private Color initialIconColor;

        #region Lifecycle Events
        private void Awake()
        {
            SetupPointerDetection();
            ToggleLock(true);

            // Debug: Log button info
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null && hotkeyNumber == 1)
            {
                Debug.Log($"First Button - Size: {rect.rect.size}, Position: {rect.anchoredPosition}, Pivot: {rect.pivot}");
            }
        }

        private void SetupPointerDetection()
        {
            // Ensure root GameObject has Image component for pointer detection
            Image buttonImage = GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = gameObject.AddComponent<Image>();
            }

            // Make it transparent but receive raycasts
            buttonImage.color = new Color(1f, 1f, 1f, 0f);
            buttonImage.raycastTarget = true;

            // Disable raycast targets on child objects
            Image[] childImages = GetComponentsInChildren<Image>();
            foreach (Image img in childImages)
            {
                if (img != buttonImage) // Don't disable the root image
                {
                    img.raycastTarget = false;
                }
            }
        }

        private void OnDestroy()
        {
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            // Don't process hotkeys during enemy turn
            if (TurnManager.Instance != null && TurnManager.Instance.CurrentTurn != TurnManager.TurnState.EnemyTurn)
            {
                // Check if this button's hotkey was pressed
                // Only check if hotkeyNumber has a value and skill is not locked
                if (hotkeyNumber.HasValue && !isLocked && IsHotkeyPressed(hotkeyNumber.Value))
                {
                    HandleClicked();
                }
            }
        }

        private bool IsHotkeyPressed(int hotkey)
        {
            if (hotkey < 1 || hotkey > 9)
                return false;

            // Create array of key pairs for cleaner code
            var keyPairs = new[]
            {
        (Keyboard.current.digit1Key, Keyboard.current.numpad1Key),
        (Keyboard.current.digit2Key, Keyboard.current.numpad2Key),
        (Keyboard.current.digit3Key, Keyboard.current.numpad3Key),
        (Keyboard.current.digit4Key, Keyboard.current.numpad4Key),
        (Keyboard.current.digit5Key, Keyboard.current.numpad5Key),
        (Keyboard.current.digit6Key, Keyboard.current.numpad6Key),
        (Keyboard.current.digit7Key, Keyboard.current.numpad7Key),
        (Keyboard.current.digit8Key, Keyboard.current.numpad8Key),
        (Keyboard.current.digit9Key, Keyboard.current.numpad9Key),
    };

            int index = hotkey - 1; // Convert hotkey (1-9) to array index (0-8)
            if (index < 0 || index >= keyPairs.Length)
                return false;

            var (digitKey, numpadKey) = keyPairs[index];
            return digitKey.wasPressedThisFrame || numpadKey.wasPressedThisFrame;
        }
        #endregion

        #region Event Handlers
        // Click detection
        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if it's player turn
            if (TurnManager.Instance != null && TurnManager.Instance.CurrentTurn != TurnManager.TurnState.PlayerTurn)
            {
                return;
            }

            Debug.Log($"SkillButtonUI: OnPointerClick called for {gameObject.name}");
            HandleClicked();
        }


        // Hover detection - always fire, even for locked buttons
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Check if it's player turn
            if (TurnManager.Instance != null && TurnManager.Instance.CurrentTurn != TurnManager.TurnState.PlayerTurn)
            {
                return;
            }

            Debug.Log($"SkillButtonUI: OnPointerEnter called for {gameObject.name}");

            // Don't show hover visual effects if skill is locked (not enough stamina)
            // But still allow tooltip to show
            if (!isLocked && !isPressed)
            {
                UpdateHoverState(true);
            }

            // Show tooltip if skill exists (always show, even when locked or pressed)
            if (boundSkill != null && tooltipPrefab != null)
            {
                // Destroy any existing tooltip
                if (currentTooltipInstance != null)
                {
                    Destroy(currentTooltipInstance.gameObject);
                }

                // Parent tooltip to this button (as a child)
                GameObject tooltipObj = Instantiate(tooltipPrefab, transform);
                tooltipObj.SetActive(true);

                currentTooltipInstance = tooltipObj.GetComponent<SkillTooltip>();

                if (currentTooltipInstance != null)
                {
                    // Pass the button's RectTransform for positioning
                    RectTransform buttonRect = GetComponent<RectTransform>();
                    currentTooltipInstance.ShowTooltip(boundSkill, buttonRect);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"SkillButtonUI: OnPointerExit called for {gameObject.name}");

            // Allow tooltip to hide even when pressed, but only update hover visual state if not pressed
            if (!isPressed)
            {
                UpdateHoverState(false);
            }

            // Hide and destroy tooltip (always hide, even when pressed)
            if (currentTooltipInstance != null)
            {
                currentTooltipInstance.HideTooltip();
                Destroy(currentTooltipInstance.gameObject);
                currentTooltipInstance = null;
            }
        }
        #endregion

        #region Exposed Actions
        public void BindSkill(Skill skill, int? hotkey = null)
        {
            boundSkill = skill;
            hotkeyNumber = hotkey;

            bool hasSkill = boundSkill != null;

            // Update icon
            if (iconImage != null)
            {
                iconImage.sprite = hasSkill ? boundSkill.icon : null;
                initialIconColor = iconImage.color;
                iconImage.enabled = hasSkill && boundSkill.icon != null;

                // If button is already locked, apply dull color now that we have the initial color
                if (isLocked)
                {
                    iconImage.color = new Color(
                        initialIconColor.r * 0.6f,
                        initialIconColor.g * 0.6f,
                        initialIconColor.b * 0.6f,
                        initialIconColor.a
                    );
                }
            }
        }

        public void ToggleLock(bool locked)
        {
            isLocked = locked;

            if (unlockedFrame != null)
            {
                unlockedFrame.SetActive(!isLocked);
            }

            if (lockedFrame != null)
            {
                lockedFrame.SetActive(isLocked);
            }

            // Apply duller color to icon when locked (on cooldown)
            // Only do this if initialIconColor has been set (after BindSkill is called)
            if (iconImage != null && initialIconColor.a > 0)
            {
                if (isLocked)
                {
                    // Make icon duller by reducing brightness and saturation
                    // Reduce RGB values by 40% to make it look grayed out
                    iconImage.color = new Color(
                        initialIconColor.r * 0.6f,
                        initialIconColor.g * 0.6f,
                        initialIconColor.b * 0.6f,
                        initialIconColor.a
                    );
                }
                else
                {
                    // Restore original color when unlocked
                    iconImage.color = initialIconColor;
                }
            }
        }
        #endregion

        private void HandleClicked()
        {
            Debug.LogWarning($"SkillButtonUI: HandleClicked called for {gameObject.name}");
            if (boundSkill == null)
                return;

            UpdateSelectedState(!isPressed);

            OnSkillInvoked?.Invoke(this);

        }

        #region Visual State Updates
        public void UpdateHoverState(bool state)
        {
            Debug.Log($"SkillButtonUI: UpdateHoverState called for {gameObject.name} with state {state}");

            if (state == isHovered) return;

            UpdateVisualState(state);
            isHovered = state;
        }

        public void UpdateSelectedState(bool state)
        {
            if (state == isPressed) return;

            UpdateVisualState(state);
            isPressed = state;
        }

        protected void UpdateVisualState(bool state)
        {
            Debug.Log($"SkillButtonUI: UpdateVisualState called for {gameObject.name} with state {state}");
            if (visualState == state) return;

            if (iconImage != null)
            {
                iconImage.color = state ?
                    new Color(initialIconColor.r - 0.25f, initialIconColor.g - 0.25f, initialIconColor.b - 0.25f, initialIconColor.a) :
                    new Color(initialIconColor.r + 0.25f, initialIconColor.g + 0.25f, initialIconColor.b + 0.25f, initialIconColor.a);
            }

            // Apply visual pressed effect
            if (unlockedFrame != null && !isLocked)
            {
                Image frameImage = unlockedFrame.GetComponent<Image>();
                if (frameImage != null)
                {
                    frameImage.color = state ? selectedColor : Color.white;
                }
            }

            if (lockedFrame != null && isLocked)
            {
                Image frameImage = lockedFrame.GetComponent<Image>();
                if (frameImage != null)
                {
                    frameImage.color = state ? selectedColor : Color.white;
                }
            }

            visualState = !visualState;
        }
        #endregion
    }
}

