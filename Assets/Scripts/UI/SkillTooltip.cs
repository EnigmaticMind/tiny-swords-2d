using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TinySwords2D.Data;
using TinySwords2D.Gameplay; // Add this using statement
using System.Collections;
using System.Linq; // Add this

namespace TinySwords2D.UI
{
  public class SkillTooltip : MonoBehaviour
  {
    [Header("Placeholder References (Auto-found if not assigned)")]
    [Tooltip("Text component for tooltip body/description")]
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Tooltip("Text component for skill title/name")]
    [SerializeField] private TextMeshProUGUI skillNameText;

    [Tooltip("Text component for stamina display")]
    [SerializeField] private TextMeshProUGUI tooltipStamina;

    [Header("GameObject References (Alternative)")]
    [Tooltip("Body GameObject - will search for TextMeshProUGUI in children")]
    [SerializeField] private GameObject bodyGameObject;

    [Tooltip("Chip GameObject - will search for TextMeshProUGUI in children")]
    [SerializeField] private GameObject chipGameObject;

    [Header("Settings")]
    [SerializeField] private float lockDelay = 2f; // Time before tooltip "locks"

    [Header("Layout Settings")]
    [Tooltip("Vertical offset for tooltip positioning (positive = higher)")]
    [SerializeField] private float verticalOffset = 0f;

    private Skill currentSkill;
    private bool isVisible = false;
    private bool isLocked = false;
    private float hoverTime = 0f;
    private Coroutine showCoroutine;
    private CanvasGroup canvasGroup;
    private RectTransform tooltipRect;

    private void Awake()
    {
      // Auto-find components if not assigned
      FindPlaceholderComponents();

      // Get or add CanvasGroup for visibility control
      canvasGroup = GetComponent<CanvasGroup>();
      if (canvasGroup == null)
      {
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
      }

      // Get RectTransform reference
      tooltipRect = GetComponent<RectTransform>();

      // Hide tooltip visually using CanvasGroup (keeps GameObject active so children are visible in hierarchy)
      canvasGroup.alpha = 0f;
      canvasGroup.blocksRaycasts = false;
      canvasGroup.interactable = false;

      // Disable raycast targets on all Image components in tooltip
      Image[] images = GetComponentsInChildren<Image>(true);
      foreach (Image img in images)
      {
        img.raycastTarget = false; // Prevent tooltip from blocking pointer events
      }

      // Also disable raycast targets on TextMeshProUGUI components
      TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
      foreach (TextMeshProUGUI text in texts)
      {
        text.raycastTarget = false; // Prevent text from blocking pointer events
      }
    }

    private void Update()
    {
      if (isVisible && !isLocked)
      {
        hoverTime += Time.deltaTime;
        if (hoverTime >= lockDelay)
        {
          LockTooltip();
        }
      }
    }

    public void ShowTooltip(Skill skill, RectTransform buttonRect)
    {
      if (skill == null || buttonRect == null) return;

      currentSkill = skill;
      hoverTime = 0f;
      isLocked = false;

      // Cancel any existing show coroutine
      if (showCoroutine != null)
      {
        StopCoroutine(showCoroutine);
      }

      showCoroutine = StartCoroutine(ShowTooltipImmediate(skill, buttonRect));
    }

    private IEnumerator ShowTooltipImmediate(Skill skill, RectTransform buttonRect)
    {
      if (skill == null || buttonRect == null) yield break;

      // Find components if not already found
      FindPlaceholderComponents();

      // Parse and format tooltip text
      string formattedText = FormatTooltipText(skill.tooltipText, skill);

      // Update UI
      if (tooltipText != null)
      {
        tooltipText.text = formattedText;
      }
      else
      {
        Debug.LogWarning("SkillTooltip: tooltipText is null!");
      }

      if (skillNameText != null)
      {
        skillNameText.text = skill.skillName;
      }
      else
      {
        Debug.LogWarning("SkillTooltip: skillNameText is null!");
      }

      // Update stamina display
      UpdateStaminaDisplay(skill);

      // GameObject should already be active (we're using CanvasGroup instead)
      // No need to call SetActive(true)

      // Hide visually using CanvasGroup while we position
      if (canvasGroup != null)
      {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
      }

      // Position tooltip off-screen initially to ensure layout can calculate
      // This prevents it from appearing in the wrong place
      if (tooltipRect != null)
      {
        tooltipRect.anchoredPosition = new Vector2(-10000, -10000);
      }

      // Wait for layout to update
      yield return null;

      // Force layout update
      if (tooltipRect != null)
      {
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
      }

      // Wait another frame to ensure layout is fully calculated
      yield return null;

      // Wait one more frame to ensure all layout calculations are complete
      yield return null;

      // Now position tooltip correctly BEFORE making it visible
      PositionTooltip(buttonRect);

      // Wait one final frame after positioning to ensure position is set
      yield return null;

      // Now make it visible - it's fully positioned
      if (canvasGroup != null)
      {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false; // Keep false so tooltip doesn't block button hover
        canvasGroup.interactable = false;
      }

      // Mark as visible
      isVisible = true;
    }

    private void FindPlaceholderComponents()
    {
      // If GameObjects are assigned, find TextMeshProUGUI in them
      if (tooltipText == null && bodyGameObject != null)
      {
        tooltipText = bodyGameObject.GetComponentInChildren<TextMeshProUGUI>(true);
      }

      if (skillNameText == null && chipGameObject != null)
      {
        skillNameText = chipGameObject.GetComponentInChildren<TextMeshProUGUI>(true);
      }

      // Find tooltipStamina - search for it by name
      if (tooltipStamina == null)
      {
        Transform staminaObj = transform.Find("tooltipStamina");
        if (staminaObj != null)
        {
          tooltipStamina = staminaObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
          // Fallback: search in SkillBody
          Transform skillBodyObj = transform.Find("SkillBody");
          if (skillBodyObj != null)
          {
            staminaObj = skillBodyObj.Find("tooltipStamina");
            if (staminaObj != null)
            {
              tooltipStamina = staminaObj.GetComponent<TextMeshProUGUI>();
            }
          }
          
          // Last resort: search all children
          if (tooltipStamina == null)
          {
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
              if (text.name.Contains("Stamina") || text.name.Contains("stamina"))
              {
                tooltipStamina = text;
                break;
              }
            }
          }
        }
      }

      // Fallback to name-based search if still null
      if (tooltipText == null)
      {
        Transform textObj = transform.Find("TooltipText");
        if (textObj != null)
        {
          tooltipText = textObj.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        else
        {
          TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
          foreach (var text in allTexts)
          {
            if (!text.name.Contains("Name") && !text.name.Contains("Title"))
            {
              tooltipText = text;
              break;
            }
          }
        }
      }

      if (skillNameText == null)
      {
        Transform nameObj = transform.Find("SkillNameText");
        if (nameObj != null)
        {
          skillNameText = nameObj.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        else
        {
          TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
          foreach (var text in texts)
          {
            if (text != tooltipText && (text.name.Contains("Name") || text.name.Contains("Title")))
            {
              skillNameText = text;
              break;
            }
          }
        }
      }

      // Debug warnings
      if (tooltipText == null)
      {
        Debug.LogWarning("SkillTooltip: Could not find tooltipText component. Tooltip will not display description text.");
      }
      if (skillNameText == null)
      {
        Debug.LogWarning("SkillTooltip: Could not find skillNameText component. Tooltip will work but name won't display.");
      }
      if (tooltipStamina == null)
      {
        Debug.LogWarning("SkillTooltip: Could not find tooltipStamina component. Stamina will not display.");
      }
    }

    public void HideTooltip()
    {
      if (showCoroutine != null)
      {
        StopCoroutine(showCoroutine);
        showCoroutine = null;
      }

      // Hide visually immediately
      if (canvasGroup != null)
      {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
      }

      isVisible = false;
      isLocked = false;
      hoverTime = 0f;
      currentSkill = null;

      // Don't deactivate - keep GameObject active so children are visible in hierarchy
      // Remove the DeactivateAfterDelay coroutine call
    }

    private string FormatTooltipText(string template, Skill skill)
    {
      if (string.IsNullOrEmpty(template))
      {
        // Fallback to generating from skill data
        return GenerateDefaultTooltip(skill);
      }

      // Replace placeholders with actual values
      string result = template;

      // Remove lines containing damage placeholders when the value is 0
      // This prevents showing "Deals 0 damage" for skills that don't deal damage
      if (skill.targetDamage == 0)
      {
        // Remove lines that contain {targetDamage} placeholder
        result = RemoveLinesContaining(result, "{targetDamage}");
      }

      if (skill.selfDamage == 0)
      {
        // Remove lines that contain {selfDamage} placeholder
        result = RemoveLinesContaining(result, "{selfDamage}");
      }

      // Replace damage values (show as positive numbers) - only if not 0 (already handled above)
      if (skill.targetDamage != 0)
      {
        result = result.Replace("{targetDamage}", Mathf.Abs(skill.targetDamage).ToString());
      }
      else
      {
        result = result.Replace("{targetDamage}", "");
      }

      result = result.Replace("{targetArmor}", skill.targetArmor.ToString());

      if (skill.selfDamage != 0)
      {
        result = result.Replace("{selfDamage}", Mathf.Abs(skill.selfDamage).ToString());
      }
      else
      {
        result = result.Replace("{selfDamage}", "");
      }

      result = result.Replace("{selfArmor}", skill.selfArmor.ToString());
      // Replace {cooldown} with {staminaRequirement}
      result = result.Replace("{staminaRequirement}", skill.staminaRequirement > 0 ? skill.staminaRequirement.ToString() : "None");

      // Replace target type
      result = result.Replace("{targetType}", FormatTargetType(skill.targetType));

      // Replace skill type
      result = result.Replace("{skillType}", skill.skillType.ToString());

      // Replace damage reduction
      if (skill.damageReduction > 0)
      {
        result = result.Replace("{damageReduction}", skill.damageReduction.ToString());
      }
      else
      {
        result = result.Replace("{damageReduction}", "");
      }

      // Replace cancels action
      if (skill.cancelsAction)
      {
        result = result.Replace("{cancelsAction}", "Cancels target's action");
      }
      else
      {
        result = result.Replace("{cancelsAction}", "");
      }

      // Replace armor-only
      if (skill.armorOnly)
      {
        result = result.Replace("{armorOnly}", "Armor only");
      }
      else
      {
        result = result.Replace("{armorOnly}", "");
      }

      // Replace intercepts attack
      if (skill.interceptsAttack)
      {
        result = result.Replace("{interceptsAttack}", "Forces target to attack caster");
      }
      else
      {
        result = result.Replace("{interceptsAttack}", "");
      }

      // Clean up any empty lines or extra whitespace
      result = CleanupEmptyLines(result);

      return result;
    }

    private string RemoveLinesContaining(string text, string searchString)
    {
      if (string.IsNullOrEmpty(text)) return text;

      string[] lines = text.Split('\n');
      System.Collections.Generic.List<string> filteredLines = new System.Collections.Generic.List<string>();

      foreach (string line in lines)
      {
        if (!line.Contains(searchString))
        {
          filteredLines.Add(line);
        }
      }

      return string.Join("\n", filteredLines);
    }

    private string CleanupEmptyLines(string text)
    {
      if (string.IsNullOrEmpty(text)) return text;

      string[] lines = text.Split('\n');
      System.Collections.Generic.List<string> cleanedLines = new System.Collections.Generic.List<string>();

      foreach (string line in lines)
      {
        string trimmed = line.Trim();
        // Keep non-empty lines, or lines that are just whitespace but might be intentional spacing
        if (!string.IsNullOrEmpty(trimmed) || cleanedLines.Count == 0 || !string.IsNullOrEmpty(cleanedLines[cleanedLines.Count - 1].Trim()))
        {
          cleanedLines.Add(line);
        }
      }

      return string.Join("\n", cleanedLines).Trim();
    }

    private string GenerateDefaultTooltip(Skill skill)
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();

      if (skill.targetDamage != 0)
      {
        sb.AppendLine(skill.targetDamage > 0
            ? $"Deals {skill.targetDamage} damage"
            : $"Heals {Mathf.Abs(skill.targetDamage)} health");
      }

      if (skill.targetArmor != 0)
      {
        sb.AppendLine(skill.targetArmor > 0
            ? $"Grants {skill.targetArmor} armor"
            : $"Reduces {Mathf.Abs(skill.targetArmor)} armor");
      }

      if (skill.selfDamage != 0)
      {
        sb.AppendLine(skill.selfDamage > 0
            ? $"Self: {skill.selfDamage} damage"
            : $"Self: Heals {Mathf.Abs(skill.selfDamage)}");
      }

      if (skill.selfArmor != 0)
      {
        sb.AppendLine(skill.selfArmor > 0
            ? $"Self: +{skill.selfArmor} armor"
            : $"Self: {skill.selfArmor} armor");
      }

      // Add damage reduction info
      if (skill.damageReduction > 0)
      {
        sb.AppendLine($"Reduces target damage by {skill.damageReduction}");
      }

      // Add cancel action info
      if (skill.cancelsAction)
      {
        sb.AppendLine("Cancels target's action");
      }

      // Add armor-only info
      if (skill.armorOnly)
      {
        sb.AppendLine("Only affects armor");
      }

      // Add intercept info
      if (skill.interceptsAttack)
      {
        sb.AppendLine("Forces target to attack caster");
      }

      // Update the detailed info section
      if (skill.staminaRequirement > 0)
      {
        sb.AppendLine($"Stamina Required: {skill.staminaRequirement}");
      }

      return sb.ToString().TrimEnd();
    }

    private string FormatTargetType(SkillTargetType targetType)
    {
      switch (targetType)
      {
        case SkillTargetType.Self: return "Self";
        case SkillTargetType.Ally: return "Ally";
        case SkillTargetType.Enemy: return "Enemy";
        case SkillTargetType.AllAllies: return "All Allies";
        case SkillTargetType.AllEnemies: return "All Enemies";
        default: return targetType.ToString();
      }
    }

    private void PositionTooltip(RectTransform buttonRect)
    {
      if (tooltipRect == null || buttonRect == null)
      {
        Debug.LogError("SkillTooltip: Missing RectTransform!");
        return;
      }

      // Get button's dimensions and pivot
      float buttonHeight = buttonRect.rect.height;
      float buttonWidth = buttonRect.rect.width;
      Vector2 buttonPivot = buttonRect.pivot;

      // Calculate button's top edge Y position
      float buttonTopY = buttonHeight * (1f - buttonPivot.y);
      float buttonLeftX = -buttonWidth * buttonPivot.x;

      // Get tooltip dimensions and pivot
      Vector2 tooltipPivot = tooltipRect.pivot;
      float tooltipWidth = tooltipRect.rect.width;
      float tooltipHeight = tooltipRect.rect.height;

      // Position tooltip so its bottom-left is at button's top-left
      Vector2 tooltipPos = new Vector2(
        buttonLeftX - (tooltipPivot.x * tooltipWidth),
        buttonTopY + (tooltipPivot.y * tooltipHeight) + verticalOffset  // Add configurable offset
      );

      tooltipRect.anchoredPosition = tooltipPos;
    }

    private void LockTooltip()
    {
      isLocked = true;
      // Add visual indicator (border fill animation, etc.)
    }

    private void OnDestroy()
    {
      // Clean up any remaining tooltip instance
      // This script is now on the prefab, so no explicit cleanup here
    }

    /// <summary>
    /// Updates the stamina display with current and maximum stamina values.
    /// </summary>
    private void UpdateStaminaDisplay(Skill skill)
    {
      if (tooltipStamina == null || skill == null) return;

      // Get the active character instance to check current stamina
      CharacterInstance activeCharacter = null;
      
      // Use CharacterRoster.Instance (it's a singleton)
      if (CharacterRoster.Instance != null)
      {
        activeCharacter = CharacterRoster.Instance.ActiveCharacterInstance;
      }

      if (activeCharacter != null)
      {
        int currentStamina = activeCharacter.GetSkillStamina(skill);
        int maxStamina = skill.staminaRequirement;
        tooltipStamina.text = $"Stamina: {currentStamina}/{maxStamina}";
      }
      else
      {
        // Fallback if no character found
        tooltipStamina.text = $"Stamina: {skill.staminaRequirement}/{skill.staminaRequirement}";
      }
    }
  }
}
