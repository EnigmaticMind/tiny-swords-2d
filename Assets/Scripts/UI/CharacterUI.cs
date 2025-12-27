using UnityEngine;
using TMPro;
using System.Linq;
using TinySwords2D.Gameplay;
using TinySwords2D.Data;
using TinySwords2D.UI;
using UnityEngine.EventSystems; // Add this line

/// <summary>
/// Reads from CharacterInstance (source of truth) and updates UI automatically.
/// Polls CharacterInstance each frame to keep UI in sync.
/// </summary>
public class CharacterUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
  [Header("Auto-Find Settings")]
  [Tooltip("If true, automatically finds TextMeshPro components by common names")]
  [SerializeField] private bool autoFindTexts = true;

  [Header("Selector Settings")]
  [Tooltip("The Selector GameObject with 4 corner children")]
  [SerializeField] private GameObject selectorObject;
  [Tooltip("Auto-find Selector if not assigned")]
  [SerializeField] private bool autoFindSelector = true;

  [Header("Manual Assignments (Optional)")]
  [SerializeField] private TextMeshProUGUI healthText;
  [SerializeField] private TextMeshProUGUI armorText;
  [SerializeField] private TextMeshProUGUI statusText;
  [SerializeField] private TextMeshProUGUI attackPlanText;

  // Add these new fields:
  [Header("Hit Indicator")]
  [SerializeField] private GameObject hitIcon;
  [SerializeField] private TextMeshProUGUI hitText;

  private CharacterInstance characterInstance;
  private SpriteRenderer[] selectorCorners; // Array of 4 corner sprites

  // Cache last values to avoid unnecessary updates
  private int lastHealth = -1;
  private int lastArmor = -1;
  private string lastStatus = "";
  private string lastAttackPlan = null; // Change from "" to null so empty string changes are detected
  private int lastIncomingDamage = -1; // Add this to cache incoming damage

  private bool isSubscribed = false;

  // Remove SkillState and currentSkillState
  // private SkillState currentSkillState; // Remove this
  private bool isMouseOver = false;

  private void Awake()
  {
    characterInstance = GetComponentInParent<CharacterInstance>();

    if (autoFindTexts)
    {
      // Auto-find by searching children
      if (healthText == null) healthText = FindText("Health");
      if (armorText == null) armorText = FindText("Armor");
      if (statusText == null) statusText = FindText("Status");
      if (attackPlanText == null) attackPlanText = FindText("Plan");
    }

    // Auto-find HitIcon and Hit text
    if (hitIcon == null)
    {
      Transform hitIconTransform = transform.Find("HitIcon");
      if (hitIconTransform != null)
      {
        hitIcon = hitIconTransform.gameObject;
      }
    }

    if (hitText == null && hitIcon != null)
    {
      Transform hitTransform = hitIcon.transform.Find("Hit");
      if (hitTransform != null)
      {
        hitText = hitTransform.GetComponent<TextMeshProUGUI>();
      }
    }

    // Hide hit icon by default
    if (hitIcon != null)
    {
      hitIcon.SetActive(false);
    }

    // Setup selector
    SetupSelector();
  }

  private void Start()
  {
    // Try subscribing again in case SkillBarController wasn't ready in OnEnable
    // SubscribeToSkillEvents(); // Remove this

  }

  // Remove HandleSkillStateChanged and subscription code
  // private void HandleSkillStateChanged(SkillState state) { ... } // Remove this
  // private void SubscribeToSkillEvents() { ... } // Remove this

  private void OnEnable()
  {
    // Subscribe to skill hover/selection events
    // SubscribeToSkillEvents(); // Remove this
  }

  private void OnDisable()
  {
    // Unsubscribe
    if (SkillBarController.Instance != null)
    {
      // SkillBarController.Instance.OnSkillStateChanged -= HandleSkillStateChanged; // Remove this
      Debug.Log("CharacterUI: Unsubscribed from SkillBarController events");
    }
  }

  private void Update()
  {
    // Try to subscribe if not already subscribed
    if (!isSubscribed && SkillBarController.Instance != null)
    {
      // SubscribeToSkillEvents(); // Remove this
      isSubscribed = true;
    }

    // Poll CharacterInstance (source of truth) and update UI
    if (characterInstance == null) return;

    // Update health
    if (characterInstance.currentHealth != lastHealth)
    {
      Debug.Log($"CharacterUI: Updating health display for {characterInstance.GetCharacterName()} - Current: {characterInstance.currentHealth}, Last: {lastHealth}");
      lastHealth = characterInstance.currentHealth;
      UpdateHealthDisplay();
    }

    // Update armor
    if (characterInstance.currentArmor != lastArmor)
    {
      lastArmor = characterInstance.currentArmor;
      UpdateArmorDisplay();
    }

    // Update status
    if (characterInstance.currentStatus != lastStatus)
    {
      lastStatus = characterInstance.currentStatus;
      UpdateStatusDisplay();
    }

    // Update attack plan
    if (characterInstance.attackPlan != lastAttackPlan)
    {
      lastAttackPlan = characterInstance.attackPlan;
      UpdateAttackPlanDisplay();
    }

    // Update selector based on button states
    UpdateSelector();

    // Hide UI elements during enemy turn
    bool isPlayerTurn = TurnManager.Instance != null &&
                        TurnManager.Instance.CurrentTurn == TurnManager.TurnState.PlayerTurn;

    // Hide attack plan during enemy turn
    if (attackPlanText != null)
    {
      attackPlanText.gameObject.SetActive(isPlayerTurn);
    }

    // Update incoming damage indicator for all characters
    // (UpdateHitIndicator will hide it for enemies)
    int incomingDamage = 0;
    if (characterInstance != null && characterInstance.gameObject.CompareTag("Player"))
    {
      // Only calculate damage during player turn
      if (isPlayerTurn)
      {
        incomingDamage = CalculateIncomingDamage();
      }
    }

    // Always update the indicator (it will hide for enemies and during enemy turn)
    if (incomingDamage != lastIncomingDamage)
    {
      lastIncomingDamage = incomingDamage;
      UpdateHitIndicator(incomingDamage);
    }
  }

  private TextMeshProUGUI FindText(string nameContains)
  {
    return GetComponentsInChildren<TextMeshProUGUI>()
      .FirstOrDefault(t => t.name.Contains(nameContains));
  }

  private void UpdateHealthDisplay()
  {
    if (healthText != null && characterInstance != null && characterInstance.Definition != null)
    {
      healthText.text = $"{characterInstance.currentHealth}/{characterInstance.Definition.maxHealth}";
    }
  }

  private void UpdateArmorDisplay()
  {
    if (armorText != null && characterInstance != null)
    {
      armorText.text = $"{characterInstance.currentArmor}";
    }
  }

  private void UpdateStatusDisplay()
  {
    if (statusText != null && characterInstance != null)
    {
      statusText.text = $"Status\n{characterInstance.currentStatus}";
    }
  }

  private void UpdateAttackPlanDisplay()
  {
    if (attackPlanText != null && characterInstance != null)
    {
      attackPlanText.text = characterInstance.attackPlan;
    }
  }

  private void UpdateSelector()
  {
    // Skip if skill is executing, ALWAYS!
    if (SkillBarController.Instance != null && SkillBarController.Instance.isExecutingSkill)
    {
      SetSelectorOpacity(0f);
      return;
    }


    if (selectorCorners == null || selectorCorners.Length == 0) return;

    float targetOpacity = 0f;

    // Query SkillBarController for active skill (from button states)
    Skill activeSkill = GetActiveSkillFromButtons();

    if (activeSkill != null)
    {
      Debug.Log($"CharacterUI: Active skill: {activeSkill.skillName}, Is Mouse Over: {isMouseOver}");

      // Check if this character is a valid target
      bool isValidTarget = IsValidTarget(activeSkill);

      if (isValidTarget)
      {
        bool isSelected = IsSkillSelected(activeSkill); // Check if skill is selected (pressed), not just hovered

        if (activeSkill.targetType == SkillTargetType.Self ||
            activeSkill.targetType == SkillTargetType.AllAllies ||
            activeSkill.targetType == SkillTargetType.AllEnemies)
        {
          // All targets or skills = 100% opacity
          targetOpacity = 1f;
        }
        else if (isMouseOver && isSelected)
        {
          targetOpacity = 1f;
        }
        else
        {
          targetOpacity = 0.7f;
        }
      }
    }

    SetSelectorOpacity(targetOpacity);
  }

  private bool IsValidTarget(Skill skill)
  {
    CharacterInstance caster = CharacterRoster.Instance?.ActiveCharacterInstance;
    return TargetingUtility.IsValidTarget(skill, characterInstance, caster);
  }

  private void SetSelectorOpacity(float opacity)
  {
    if (selectorCorners == null) return;
    Debug.Log($"CharacterUI: Setting selector opacity to {opacity}");

    foreach (SpriteRenderer corner in selectorCorners)
    {
      if (corner != null)
      {
        Color color = corner.color;
        color.a = opacity;
        corner.color = color;
      }
    }
  }

  private void SetupSelector()
  {
    // Find selector GameObject
    if (selectorObject == null && autoFindSelector)
    {
      Transform selectorTransform = transform.Find("Selector");
      if (selectorTransform != null)
      {
        selectorObject = selectorTransform.gameObject;
      }
    }

    if (selectorObject != null)
    {
      // Get all SpriteRenderer children (the 4 corners)
      selectorCorners = selectorObject.GetComponentsInChildren<SpriteRenderer>();

      // Hide selector by default
      SetSelectorOpacity(0f);

      Debug.Log($"CharacterUI: Setup selector with {selectorCorners?.Length ?? 0} corners");
    }
    else
    {
      Debug.LogWarning("CharacterUI: Selector GameObject not found!");
    }
  }

  // Event handlers
  // Remove HandleSkillStateChanged and subscription code
  // private void HandleSkillStateChanged(SkillState state) { ... } // Remove this
  public void OnPointerEnter(PointerEventData eventData)
  {
    Debug.Log($"CharacterUI: OnPointerEnter called on {gameObject.name}");
    isMouseOver = true;
    UpdateSelector();
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    Debug.Log($"CharacterUI: OnPointerExit called on {gameObject.name}");
    isMouseOver = false;
    UpdateSelector();
  }

  private Skill GetActiveSkillFromButtons()
  {
    if (SkillBarController.Instance == null) return null;

    // Check for pressed button first (selected), then hovered
    foreach (var button in SkillBarController.Instance.ButtonPool.Values)
    {
      if (button != null && button.isPressed && button.BoundSkill != null)
      {
        return button.BoundSkill;
      }
    }

    // If no pressed button, check for hovered
    foreach (var button in SkillBarController.Instance.ButtonPool.Values)
    {
      if (button != null && button.isHovered && button.BoundSkill != null)
      {
        return button.BoundSkill;
      }
    }

    return null;
  }

  private bool IsSkillSelected(Skill skill)
  {
    if (SkillBarController.Instance == null || skill == null) return false;

    // Check if any button with this skill is pressed
    foreach (var button in SkillBarController.Instance.ButtonPool.Values)
    {
      if (button != null && button.BoundSkill == skill && button.isPressed)
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Calculates total incoming damage from all enemy planned moves targeting this character
  /// </summary>
  private int CalculateIncomingDamage()
  {
    if (characterInstance == null) return 0;

    int totalDamage = 0;
    EnemyAI[] allEnemyAIs = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

    foreach (EnemyAI enemyAI in allEnemyAIs)
    {
      if (enemyAI == null || !enemyAI.HasPlannedMove()) continue;

      EnemyAI.PlannedMove plannedMove = enemyAI.GetPlannedMove();
      if (plannedMove == null || plannedMove.skill == null) continue;

      // Get the enemy character instance (caster)
      CharacterInstance enemyCharacter = enemyAI.CharacterInstance;
      if (enemyCharacter == null) continue;

      // Check if this player is the target
      if (plannedMove.target == characterInstance)
      {
        // Add target damage (positive = damage, negative = healing)
        if (plannedMove.skill.targetDamage > 0)
        {
          // Apply damage reduction from the enemy (caster) - this reduces the damage they deal
          int finalDamage = Mathf.Max(0, plannedMove.skill.targetDamage - enemyCharacter.damageReduction);
          totalDamage += finalDamage;
        }
      }
      // Check for AoE skills that target all players
      else if (plannedMove.skill.targetType == SkillTargetType.AllAllies ||
               plannedMove.skill.targetType == SkillTargetType.AllEnemies)
      {
        // For enemies, AllEnemies targets all players
        if (plannedMove.skill.targetType == SkillTargetType.AllEnemies &&
            characterInstance.gameObject.CompareTag("Player"))
        {
          if (plannedMove.skill.targetDamage > 0)
          {
            // Apply damage reduction from the enemy (caster)
            int finalDamage = Mathf.Max(0, plannedMove.skill.targetDamage - enemyCharacter.damageReduction);
            totalDamage += finalDamage;
          }
        }
      }
    }

    return totalDamage;
  }

  /// <summary>
  /// Updates the hit indicator visibility and text
  /// </summary>
  private void UpdateHitIndicator(int incomingDamage)
  {
    if (hitIcon == null) return;

    // Only show for players with incoming damage, and only during player turn
    bool isPlayerTurn = TurnManager.Instance != null &&
                        TurnManager.Instance.CurrentTurn == TurnManager.TurnState.PlayerTurn;
    bool isPlayer = characterInstance != null && characterInstance.gameObject.CompareTag("Player");
    bool shouldShow = isPlayerTurn && isPlayer && incomingDamage > 0;

    hitIcon.SetActive(shouldShow);

    // Always update text (even when hidden, in case it becomes visible)
    if (hitText != null)
    {
      if (shouldShow)
      {
        hitText.text = incomingDamage.ToString();
      }
      else
      {
        hitText.text = ""; // Clear text when hidden
      }
    }
  }
}
