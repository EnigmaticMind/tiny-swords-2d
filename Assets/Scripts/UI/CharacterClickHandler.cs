using UnityEngine;
using UnityEngine.EventSystems;
using TinySwords2D.Gameplay;
using TinySwords2D.Data;

/// <summary>
/// Handles clicking on a character to switch the active character.
/// Add this to each character GameObject (Archer, Warrior, etc.)
/// </summary>
[RequireComponent(typeof(CharacterInstance))]
public class CharacterClickHandler : MonoBehaviour, IPointerClickHandler
{
  [Header("References")]
  [SerializeField] private CharacterRoster roster;
  [SerializeField] private bool autoFindRoster = true;

  private CharacterInstance characterInstance;

  private void Awake()
  {
    characterInstance = GetComponent<CharacterInstance>();

    if (characterInstance == null)
    {
      Debug.LogError($"CharacterClickHandler: No CharacterInstance found on {gameObject.name}!");
    }

    if (autoFindRoster && roster == null)
    {
      roster = FindFirstObjectByType<CharacterRoster>();
    }

    if (roster == null)
    {
      Debug.LogWarning($"CharacterClickHandler: No CharacterRoster found! Character switching won't work.");
    }
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    // Check if it's player turn - disable during enemy turn
    if (TurnManager.Instance != null && TurnManager.Instance.CurrentTurn != TurnManager.TurnState.PlayerTurn)
    {
      return;
    }

    if (characterInstance == null || characterInstance.Definition == null)
    {
      Debug.LogWarning($"CharacterClickHandler: Cannot switch - CharacterInstance or Definition is null");
      return;
    }

    // Check if we're in targeting mode (actively selecting a target)
    if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting())
    {
      // Check if this character is a valid target for the pending skill
      Skill pendingSkill = TargetingManager.Instance.GetPendingSkill();
      CharacterInstance caster = TargetingManager.Instance.GetCaster();

      if (pendingSkill != null && caster != null)
      {
        // Check if this character is a valid target
        bool isValidTarget = TargetingUtility.IsValidTarget(pendingSkill, characterInstance, caster);

        if (isValidTarget)
        {
          // Valid target - select it
          TargetingManager.Instance.SelectTarget(characterInstance);
          return;
        }
        else
        {
          // Invalid target - cancel targeting and allow character switching
          TargetingManager.Instance.CancelTargeting();
          // Continue to character switching logic below
        }
      }
      else
      {
        // No pending skill info - just cancel targeting
        TargetingManager.Instance.CancelTargeting();
        // Continue to character switching logic below
      }
    }

    // Check if character has already acted this turn
    if (characterInstance.hasActedThisTurn)
    {
      Debug.Log($"CharacterClickHandler: {characterInstance.GetCharacterName()} has already acted this turn");
      return;
    }

    // Otherwise, switch active character (normal behavior)
    // This works even if a skill is selected (isPressed), as long as we're not in targeting mode
    if (roster == null)
    {
      Debug.LogWarning($"CharacterClickHandler: Cannot switch - CharacterRoster is null");
      return;
    }

    Debug.Log($"CharacterClickHandler: Clicked on {characterInstance.Definition.characterName}");
    roster.SetActiveCharacter(characterInstance);
  }

}
