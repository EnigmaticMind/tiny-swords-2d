using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TinySwords2D.UI;
using TinySwords2D.Data;
using TMPro;
using UnityEngine.EventSystems; // Add this

namespace TinySwords2D.Gameplay
{
  public class TurnManager : MonoBehaviour
  {
    public static TurnManager Instance { get; private set; }

    public enum TurnState
    {
      PlayerTurn,
      EnemyTurn
    }

    public TurnState CurrentTurn { get; private set; } = TurnState.PlayerTurn;

    public event Action<TurnState> OnTurnChanged;

    private List<EnemyAI> enemyAIs = new List<EnemyAI>();
    private HashSet<CharacterInstance> actedCharactersThisTurn = new HashSet<CharacterInstance>();

    [Header("Turn Indicator UI")]
    [Tooltip("Text component for displaying 'Your Turn'")]
    [SerializeField] private TextMeshProUGUI playerTurnText;

    [Tooltip("Text component for displaying 'Enemy Turn'")]
    [SerializeField] private TextMeshProUGUI enemyTurnText;

    [Header("Turn Indicator Animation")]
    [Tooltip("Starting scale for the animation (e.g., 0.5 = starts at 50% size)")]
    [SerializeField] private float indicatorStartScale = 0.5f;

    [Tooltip("Duration of the scale animation in seconds")]
    [SerializeField] private float indicatorScaleDuration = 0.5f;

    [Tooltip("Duration of the fade-in animation in seconds")]
    [SerializeField] private float indicatorFadeDuration = 0.5f;

    [Tooltip("Easing curve for the animation")]
    [SerializeField] private AnimationCurve indicatorScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Enemy Turn Timing")]
    [Tooltip("Delay in seconds before each enemy executes their move")]
    [SerializeField] private float delayBeforeEnemyMove = 0.3f;

    [Tooltip("Delay in seconds after all enemies have acted, before ending enemy turn")]
    [SerializeField] private float delayBeforeEnemyTurnEnds = 0.5f;

    private RectTransform playerTurnRect;
    private RectTransform enemyTurnRect;
    private Coroutine currentIndicatorAnimation;

    private EventSystem eventSystem;

    private void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
      }
      else
      {
        Destroy(gameObject);
      }

      // Get EventSystem reference
      eventSystem = EventSystem.current;
      if (eventSystem == null)
      {
        eventSystem = FindFirstObjectByType<EventSystem>();
      }

      // Auto-find turn indicator texts if not assigned
      InitializeTurnIndicators();
    }

    private void InitializeTurnIndicators()
    {
      // Try to find PlayerTurn and EnemyTurn child objects
      if (playerTurnText == null)
      {
        Transform playerTurnObj = transform.Find("PlayerTurn");
        if (playerTurnObj != null)
        {
          playerTurnText = playerTurnObj.GetComponent<TextMeshProUGUI>();
        }
      }

      if (enemyTurnText == null)
      {
        Transform enemyTurnObj = transform.Find("EnemyTurn");
        if (enemyTurnObj != null)
        {
          enemyTurnText = enemyTurnObj.GetComponent<TextMeshProUGUI>();
        }
      }

      // Get RectTransforms for scaling
      if (playerTurnText != null)
      {
        playerTurnRect = playerTurnText.GetComponent<RectTransform>();
      }

      if (enemyTurnText != null)
      {
        enemyTurnRect = enemyTurnText.GetComponent<RectTransform>();
      }

      // Initially hide both texts
      if (playerTurnText != null)
      {
        playerTurnText.gameObject.SetActive(false);
      }

      if (enemyTurnText != null)
      {
        enemyTurnText.gameObject.SetActive(false);
      }
    }

    private void Start()
    {
      // Find all enemies and their AI components
      FindAllEnemies();

      // Only start player turn if EncounterManager doesn't exist (for backwards compatibility)
      // If EncounterManager exists, it will call StartPlayerTurn() after spawning enemies
      if (EncounterManager.Instance == null)
      {
        // Start player turn (enemies plan their moves at the start)
        StartPlayerTurn();
      }
      else
      {
        Debug.Log("TurnManager: Waiting for EncounterManager to spawn enemies before starting turn");
      }
    }

    private void FindAllEnemies()
    {
      enemyAIs.Clear();

      // Find all enemies in scene
      CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
      List<EnemyAI> unsortedEnemies = new List<EnemyAI>();

      foreach (CharacterInstance character in allCharacters)
      {
        if (character.gameObject.CompareTag("Enemy"))
        {
          EnemyAI ai = character.GetComponent<EnemyAI>();
          if (ai == null)
          {
            ai = character.gameObject.AddComponent<EnemyAI>();
          }
          unsortedEnemies.Add(ai);
        }
      }

      // Sort enemies by Y position (top to bottom: highest Y first)
      // This ensures top enemies execute before bottom enemies
      unsortedEnemies.Sort((ai1, ai2) =>
      {
        if (ai1 == null || ai1.CharacterInstance == null) return 1;
        if (ai2 == null || ai2.CharacterInstance == null) return -1;

        float y1 = ai1.CharacterInstance.transform.position.y;
        float y2 = ai2.CharacterInstance.transform.position.y;

        // Sort descending (highest Y first = top to bottom)
        return y2.CompareTo(y1);
      });

      // Add sorted enemies to the list
      enemyAIs.AddRange(unsortedEnemies);

      Debug.Log($"TurnManager: Found {enemyAIs.Count} enemies (sorted top to bottom)");
    }

    /// <summary>
    /// Starts the player turn. Enemies plan their moves at the start of this phase.
    /// </summary>
    public void StartPlayerTurn()
    {
      CurrentTurn = TurnState.PlayerTurn;
      OnTurnChanged?.Invoke(CurrentTurn);
      Debug.Log("TurnManager: Player Turn Started - Enemies planning moves...");

      // Enable input for player turn
      EnablePlayerInput(true);

      // Show player turn indicator with animation
      ShowTurnIndicator(TurnState.PlayerTurn);

      // Reset acted status for all player characters
      actedCharactersThisTurn.Clear();
      CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
      foreach (CharacterInstance character in allCharacters)
      {
        if (character.gameObject.CompareTag("Player"))
        {
          character.hasActedThisTurn = false;
          character.attackPlan = "";
        }
        
        // Clear damage reduction for all characters at the start of a new round
        if (character.damageReduction > 0)
        {
          Debug.Log($"TurnManager: Clearing {character.GetCharacterName()}'s damage reduction ({character.damageReduction}) at start of new round");
          character.damageReduction = 0;
        }
      }

      // Have all enemies plan their moves (displayed in attackPlan)
      foreach (EnemyAI ai in enemyAIs)
      {
        if (ai != null)
        {
          // Skip dead enemies
          if (ai.CharacterInstance != null && ai.CharacterInstance.isDead)
          {
            ai.ClearPlannedMove();
            continue;
          }
          ai.PlanMove();
        }
      }
    }

    /// <summary>
    /// Marks a character as having acted this turn. Returns true if all characters have acted.
    /// </summary>
    public bool MarkCharacterAsActed(CharacterInstance character)
    {
      if (character == null || !character.gameObject.CompareTag("Player"))
      {
        return false;
      }

      if (!actedCharactersThisTurn.Contains(character))
      {
        actedCharactersThisTurn.Add(character);
        character.hasActedThisTurn = true;
        Debug.Log($"TurnManager: {character.GetCharacterName()} has acted this turn");

        // Distribute 100 stamina to this character after they complete their turn
        DistributeStaminaToCharacter(character, 100);

        // Update skill button locks after stamina distribution
        if (SkillBarController.Instance != null)
        {
          SkillBarController.Instance.UpdateSkillLocks();
        }
      }

      // Check if all player characters have acted
      return HaveAllPlayersActed();
    }

    /// <summary>
    /// Checks if all player characters have acted this turn.
    /// </summary>
    private bool HaveAllPlayersActed()
    {
      CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
      int playerCount = 0;
      int actedCount = 0;

      foreach (CharacterInstance character in allCharacters)
      {
        if (character.gameObject.CompareTag("Player"))
        {
          playerCount++;
          if (character.hasActedThisTurn)
          {
            actedCount++;
          }
        }
      }

      bool allActed = playerCount > 0 && actedCount == playerCount;
      if (allActed)
      {
        Debug.Log($"TurnManager: All {playerCount} players have acted. Ending player turn.");
      }
      return allActed;
    }

    /// <summary>
    /// Checks if a character can still act this turn.
    /// </summary>
    public bool CanCharacterAct(CharacterInstance character)
    {
      if (character == null || !character.gameObject.CompareTag("Player"))
      {
        return false;
      }
      return !character.hasActedThisTurn;
    }

    /// <summary>
    /// Ends player turn and starts enemy turn. Call this when player finishes their actions.
    /// </summary>
    public void EndPlayerTurn()
    {
      if (CurrentTurn != TurnState.PlayerTurn)
      {
        Debug.LogWarning("TurnManager: Cannot end player turn - not in player turn");
        return;
      }

      StartCoroutine(EnemyTurnCoroutine());
    }

    private IEnumerator EnemyTurnCoroutine()
    {
      CurrentTurn = TurnState.EnemyTurn;
      OnTurnChanged?.Invoke(CurrentTurn);
      Debug.Log("TurnManager: Enemy Turn Started - Executing planned moves...");

      // Clear any active targeting to prevent it from being stuck
      if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting())
      {
        TargetingManager.Instance.CancelTargeting();
      }

      // Disable input during enemy turn (except pause menu via keyboard)
      EnablePlayerInput(false);

      // Show enemy turn indicator with animation
      ShowTurnIndicator(TurnState.EnemyTurn);

      // Execute each enemy's planned move sequentially
      foreach (EnemyAI ai in enemyAIs)
      {
        if (ai != null && ai.HasPlannedMove())
        {
          // Skip dead enemies
          if (ai.CharacterInstance != null && ai.CharacterInstance.isDead)
          {
            Debug.Log($"TurnManager: Skipping {ai.CharacterInstance.GetCharacterName()} - they are dead");
            ai.ClearPlannedMove();
            continue;
          }

          // Delay before this enemy takes their turn
          if (delayBeforeEnemyMove > 0f)
          {
            yield return new WaitForSeconds(delayBeforeEnemyMove);
          }

          yield return StartCoroutine(ExecuteEnemyMove(ai));
        }
      }

      // Delay before ending enemy turn
      if (delayBeforeEnemyTurnEnds > 0f)
      {
        yield return new WaitForSeconds(delayBeforeEnemyTurnEnds);
      }

      // REMOVED: DistributeStaminaToPlayers() - now done per character after their turn

      // Re-enable input when returning to player turn
      // (StartPlayerTurn will also call EnablePlayerInput(true), but this is a safety)
      EnablePlayerInput(true);

      // Return to player turn (enemies will plan again)
      StartPlayerTurn();
    }

    private IEnumerator ExecuteEnemyMove(EnemyAI ai)
    {
      EnemyAI.PlannedMove move = ai.GetPlannedMove();
      if (move == null || move.skill == null) yield break;

      Debug.Log($"TurnManager: {ai.CharacterInstance.GetCharacterName()} executes: {move.skill.skillName}");

      // Use SkillBarController's execution logic
      if (SkillBarController.Instance != null)
      {
        yield return StartCoroutine(SkillBarController.Instance.ExecuteSkillCoroutine(
            move.skill,
            ai.CharacterInstance,
            move.target
        ));
      }
      else
      {
        Debug.LogError("TurnManager: SkillBarController.Instance is null! Cannot execute enemy move.");
      }

      // Clear the planned move
      ai.ClearPlannedMove();
    }

    /// <summary>
    /// Refreshes the enemy list. Call this when enemies are spawned/destroyed.
    /// </summary>
    public void RefreshEnemyList()
    {
      FindAllEnemies();
    }

    /// <summary>
    /// Shows the appropriate turn indicator with animation.
    /// </summary>
    private void ShowTurnIndicator(TurnState turn)
    {
      // Stop any existing animation
      if (currentIndicatorAnimation != null)
      {
        StopCoroutine(currentIndicatorAnimation);
      }

      // Start animation for the appropriate turn indicator
      if (turn == TurnState.PlayerTurn)
      {
        currentIndicatorAnimation = StartCoroutine(AnimateTurnIndicator(playerTurnText, playerTurnRect, enemyTurnText));
      }
      else
      {
        currentIndicatorAnimation = StartCoroutine(AnimateTurnIndicator(enemyTurnText, enemyTurnRect, playerTurnText));
      }
    }

    /// <summary>
    /// Animates the turn indicator appearing (scale and fade in).
    /// </summary>
    private IEnumerator AnimateTurnIndicator(TextMeshProUGUI textToShow, RectTransform rectTransform, TextMeshProUGUI textToHide)
    {
      // Hide the other text immediately (before starting animation)
      if (textToHide != null)
      {
        textToHide.gameObject.SetActive(false);
      }

      // Show the text we want to animate
      if (textToShow == null || rectTransform == null)
      {
        Debug.LogWarning("TurnManager: Turn indicator text or RectTransform is null!");
        yield break;
      }

      textToShow.gameObject.SetActive(true);

      // Set initial state (small and transparent)
      rectTransform.localScale = Vector3.one * indicatorStartScale;
      Color textColor = textToShow.color;
      textColor.a = 0f;
      textToShow.color = textColor;

      // Animate scale and opacity simultaneously
      float elapsed = 0f;
      Vector3 targetScale = Vector3.one;
      float targetAlpha = 1f;

      while (elapsed < Mathf.Max(indicatorScaleDuration, indicatorFadeDuration))
      {
        elapsed += Time.deltaTime;

        // Animate scale
        if (elapsed < indicatorScaleDuration)
        {
          float scaleT = elapsed / indicatorScaleDuration;
          float curveValue = indicatorScaleCurve.Evaluate(scaleT);
          rectTransform.localScale = Vector3.Lerp(Vector3.one * indicatorStartScale, targetScale, curveValue);
        }
        else
        {
          rectTransform.localScale = targetScale;
        }

        // Animate opacity
        if (elapsed < indicatorFadeDuration)
        {
          float fadeT = elapsed / indicatorFadeDuration;
          textColor.a = Mathf.Lerp(0f, targetAlpha, fadeT);
          textToShow.color = textColor;
        }
        else
        {
          textColor.a = targetAlpha;
          textToShow.color = textColor;
        }

        yield return null;
      }

      // Ensure final values
      rectTransform.localScale = targetScale;
      textColor.a = targetAlpha;
      textToShow.color = textColor;

      currentIndicatorAnimation = null;
    }

    /// <summary>
    /// Enables or disables player input (pointer events).
    /// Pause menu can still be opened via keyboard (ESC key).
    /// </summary>
    private void EnablePlayerInput(bool enable)
    {
      if (eventSystem != null)
      {
        eventSystem.enabled = enable;
      }
    }

    /// <summary>
    /// Randomly distributes stamina points to skills that need it for a character.
    /// </summary>
    private void DistributeStaminaToCharacter(CharacterInstance character, int totalStamina)
    {
      if (character == null || character.Definition == null || character.isDead) return;

      // Get all skills that need stamina (current < requirement)
      System.Collections.Generic.List<Skill> skillsNeedingStamina = new System.Collections.Generic.List<Skill>();

      foreach (Skill skill in character.Definition.equippedSkills)
      {
        if (skill != null && character.GetSkillStamina(skill) < skill.staminaRequirement)
        {
          skillsNeedingStamina.Add(skill);
        }
      }

      if (skillsNeedingStamina.Count == 0)
      {
        Debug.Log($"{character.GetCharacterName()}: All skills at full stamina, no distribution needed.");
        return;
      }

      // Randomly distribute stamina points
      int remainingStamina = totalStamina;
      System.Collections.Generic.List<Skill> skillsToDistribute = new System.Collections.Generic.List<Skill>(skillsNeedingStamina);

      while (remainingStamina > 0 && skillsToDistribute.Count > 0)
      {
        // Pick a random skill
        int randomIndex = UnityEngine.Random.Range(0, skillsToDistribute.Count);
        Skill selectedSkill = skillsToDistribute[randomIndex];

        // Calculate how much stamina this skill needs
        int currentStamina = character.GetSkillStamina(selectedSkill);
        int neededStamina = selectedSkill.staminaRequirement - currentStamina;

        // Give 1 stamina point (or more if you want to distribute in larger chunks)
        int staminaToGive = Mathf.Min(1, remainingStamina, neededStamina);
        character.AddSkillStamina(selectedSkill, staminaToGive);
        remainingStamina -= staminaToGive;

        // Remove skill from list if it's now full
        if (character.GetSkillStamina(selectedSkill) >= selectedSkill.staminaRequirement)
        {
          skillsToDistribute.RemoveAt(randomIndex);
        }
      }

      Debug.Log($"{character.GetCharacterName()}: Distributed {totalStamina - remainingStamina} stamina points after completing turn.");
    }
  }
}
