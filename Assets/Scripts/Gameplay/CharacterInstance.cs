using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TinySwords2D.Data;
using TinySwords2D.UI; // Add this for MenuManager

namespace TinySwords2D.Gameplay
{
  /// <summary>
  /// Component that links a GameObject to a CharacterDefinition.
  /// This is the source of truth for character runtime data.
  /// </summary>
  public class CharacterInstance : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    [Header("Character Definition")]
    [SerializeField] private CharacterDefinition characterDefinition;

    [Header("Components")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Projectile Spawn")]
    [Tooltip("Transform where projectiles should spawn from. Assign a GameObject in the scene to position it manually.")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Hover Effects")]
    [Tooltip("Hover sprite variant (e.g., warrior with glow/outline)")]
    [SerializeField] private Sprite hoverSprite;
    [Tooltip("Brightness multiplier for hover (1.0 = no change, 1.2 = 20% brighter)")]
    [SerializeField] private float hoverBrightness = 1.1f;
    [Tooltip("Use brightness effect if no hover sprite assigned")]
    [SerializeField] private bool useBrightnessFallback = true;

    [Header("Runtime Stats - Source of Truth")]
    [Tooltip("Current health. Modify this to change health.")]
    public int currentHealth;

    [Tooltip("Current armor. Modify this to change armor.")]
    public int currentArmor;

    [Tooltip("Current status effect.")]
    public string currentStatus = "Normal";

    [Tooltip("Current attack plan/intent.")]
    public string attackPlan = "";

    [Tooltip("Whether this character has acted this turn.")]
    public bool hasActedThisTurn = false;

    [Tooltip("Flat damage reduction for this character. Damage dealt is reduced by this amount.")]
    public int damageReduction = 0;

    // Stamina tracking: Dictionary of Skill -> Current Stamina
    [System.NonSerialized]
    public System.Collections.Generic.Dictionary<Skill, int> skillStamina = new System.Collections.Generic.Dictionary<Skill, int>();

    // Hover state
    private Sprite originalSprite;
    private Color originalColor;

    public bool isDead = false;

    /// <summary>
    /// The character definition assigned to this GameObject instance.
    /// </summary>
    public CharacterDefinition Definition
    {
      get => characterDefinition;
      set
      {
        characterDefinition = value;
        ApplyDefinition();
      }
    }

    public Vector3 GetProjectileSpawnPosition()
    {
      if (projectileSpawnPoint != null)
      {
        return projectileSpawnPoint.position;
      }
      return transform.position;
    }

    private void Awake()
    {
      animator = GetComponent<Animator>();
      spriteRenderer = GetComponent<SpriteRenderer>();

      // Cache original sprite and color for hover effects
      if (spriteRenderer != null)
      {
        originalSprite = spriteRenderer.sprite;
        originalColor = spriteRenderer.color;
      }

      // Apply the definition when the GameObject is created
      if (characterDefinition != null)
      {
        ApplyDefinition();
      }
    }

    private void Start()
    {
      Debug.Log($"CharacterInstance: Start called for {gameObject.name}");
      // Initialize runtime stats from definition
      if (characterDefinition != null)
      {
        currentHealth = characterDefinition.maxHealth;
        currentArmor = 0; // Initialize to 0 (can be modified by skills/effects)

        // Initialize stamina for all skills to 100% (staminaRequirement)
        InitializeSkillStamina();
      }

      // Set attack plan to empty for player characters
      if (gameObject.CompareTag("Player"))
      {
        attackPlan = "";
      }

      // Always set hasActedThisTurn to true for enemies
      if (gameObject.CompareTag("Enemy"))
      {
        hasActedThisTurn = true;
      }
    }

    private void Update()
    {
      if (currentHealth <= 0 && !isDead)
      {
        HandleDeath();
      }
    }

    private void HandleDeath()
    {
      if (isDead) return; // Already handled
      isDead = true;

      // Clear attack plan when character dies
      attackPlan = "";

      // If this is an enemy, clear their planned move so damage indicators update
      if (gameObject.CompareTag("Enemy"))
      {
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
          enemyAI.ClearPlannedMove();
        }
      }

      // Hide UI FIRST (before animation)
      CharacterUI characterUI = GetComponentInChildren<CharacterUI>();
      if (characterUI != null)
      {
        characterUI.gameObject.SetActive(false);
      }

      // Play death animation (sprite should still be visible)
      if (animator != null)
      {
        animator.SetTrigger("isDead");
        Debug.Log($"CharacterInstance: {GetCharacterName()} plays death animation: Death trigger set");

        // Start coroutine to hide sprite after animation finishes
        StartCoroutine(HideAfterDeathAnimation());
      }
      else
      {
        // If no animator, hide immediately
        if (spriteRenderer != null)
        {
          spriteRenderer.enabled = false;
        }
      }
    }

    /// <summary>
    /// Waits for death animation to finish, then hides the character sprite.
    /// </summary>
    private System.Collections.IEnumerator HideAfterDeathAnimation()
    {
      if (animator != null)
      {
        // Wait for death animation to finish
        yield return StartCoroutine(WaitForAnimationToFinish("Dead"));
      }
      else
      {
        // If no animator, wait a short time
        yield return new WaitForSeconds(1f);
      }

      Debug.Log($"{GetCharacterName()} death animation complete - sprite hidden");

      // If this is a player character, trigger game over after animation
      if (gameObject.CompareTag("Player"))
      {
        TriggerGameOver();
      }
    }

    /// <summary>
    /// Triggers game over when a player character dies.
    /// For now, restarts the game. Can be extended for future features (game over screen, stats, etc.).
    /// </summary>
    private void TriggerGameOver()
    {
      Debug.Log($"Game Over: {GetCharacterName()} has died!");

      // For now, restart the game
      // In the future, this could:
      // - Show a game over screen
      // - Save statistics
      // - Show death summary
      // - etc.
      RestartGame();
    }

    /// <summary>
    /// Restarts the game by reloading the battle scene.
    /// </summary>
    private void RestartGame()
    {
      Time.timeScale = 1f; // Always unpause before scene operations

      // Find the battle scene (the scene that contains EncounterManager or MenuManager)
      Scene battleScene = default;

      // Check all loaded scenes to find the battle scene
      for (int i = 0; i < SceneManager.sceneCount; i++)
      {
        Scene scene = SceneManager.GetSceneAt(i);

        // Skip the pause menu and skill selection scenes
        if (scene.name == "PauseMenuScene" || scene.name == "SkillSelection")
        {
          continue;
        }

        // Check if this scene has EncounterManager or MenuManager (battle scene markers)
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
          if (obj.GetComponent<EncounterManager>() != null ||
              obj.GetComponent<MenuManager>() != null ||
              obj.GetComponentInChildren<EncounterManager>() != null ||
              obj.GetComponentInChildren<MenuManager>() != null)
          {
            battleScene = scene;
            break;
          }
        }

        if (battleScene.IsValid())
        {
          break;
        }
      }

      // If we found the battle scene, reload it
      if (battleScene.IsValid())
      {
        SceneManager.LoadScene(battleScene.name);
      }
      else
      {
        // Fallback: try to find by build index
        Debug.LogWarning("CharacterInstance: Could not find battle scene, attempting fallback");

        // Try scene at index 0 (usually the main game scene)
        if (SceneManager.sceneCountInBuildSettings > 0)
        {
          SceneManager.LoadScene(0);
        }
        else
        {
          Debug.LogError("CharacterInstance: No scenes in build settings!");
        }
      }
    }

    /// <summary>
    /// Applies the character definition's data to this GameObject.
    /// You can extend this to apply stats, sprites, etc.
    /// </summary>
    private void ApplyDefinition()
    {
      if (characterDefinition == null) return;

      // Set the GameObject's name to match the character
      // Commented out to preserve scene GameObject names
      // gameObject.name = characterDefinition.characterName;

      // Optional: Apply portrait to a SpriteRenderer if you have one
      if (spriteRenderer != null && characterDefinition.portrait != null)
      {
        // Uncomment if you want the portrait to replace the sprite
        // spriteRenderer.sprite = characterDefinition.portrait;
      }
    }

    /// <summary>
    /// Plays the animation for a skill. Uses character's override if available, otherwise skill's default.
    /// </summary>
    public void PlaySkillAnimation(Skill skill)
    {
      if (animator == null)
      {
        Debug.LogWarning($"CharacterInstance: No Animator component found on {gameObject.name}");
        return;
      }

      if (skill == null)
      {
        Debug.LogWarning($"CharacterInstance: Cannot play animation for null skill");
        return;
      }

      string triggerName = characterDefinition != null
        ? characterDefinition.GetAnimationTriggerForSkill(skill)
        : skill.animationTrigger;

      if (string.IsNullOrEmpty(triggerName))
      {
        Debug.LogWarning($"CharacterInstance: No animation trigger found for skill {skill.skillName}");
        return;
      }

      animator.SetTrigger(triggerName);
      Debug.Log($"{characterDefinition?.characterName ?? "Character"} plays animation: {triggerName} for skill: {skill.skillName}");
    }

    /// <summary>
    /// Gets the character's name from the definition.
    /// </summary>
    public string GetCharacterName()
    {
      return characterDefinition != null ? characterDefinition.characterName : "Unknown";
    }

    /// <summary>
    /// Gets the character's skills from the definition.
    /// </summary>
    public System.Collections.Generic.List<Skill> GetSkills()
    {
      return characterDefinition != null ? characterDefinition.equippedSkills : new System.Collections.Generic.List<Skill>();
    }

    /// <summary>
    /// Waits for the current animation clip to finish playing.
    /// </summary>
    public System.Collections.IEnumerator WaitForCurrentAnimation()
    {
      if (animator == null)
      {
        Debug.LogWarning("CharacterInstance: No animator, cannot wait for animation");
        yield break;
      }

      // Wait for animation trigger to take effect
      yield return null;
      yield return null;

      // Get the initial state BEFORE the animation starts
      AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
      int previousHash = previousState.fullPathHash;

      // Wait for the animation state to actually change (transition to attack animation)
      int framesWaited = 0;
      while (framesWaited < 30) // Max 0.5 seconds at 60fps
      {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.fullPathHash != previousHash)
        {
          // Animation state changed - we're now in the attack animation
          Debug.Log($"CharacterInstance: Animation started. Hash: {currentState.fullPathHash}, Length: {currentState.length}, NormalizedTime: {currentState.normalizedTime}");
          break;
        }
        yield return null;
        framesWaited++;
      }

      // Now get the attack animation state
      AnimatorStateInfo attackState = animator.GetCurrentAnimatorStateInfo(0);
      int attackHash = attackState.fullPathHash;
      float animationLength = attackState.length;

      // Get the current normalizedTime (might be > 1.0 if looped)
      float startNormalizedTime = attackState.normalizedTime;

      // Calculate how many full cycles have already played
      int cyclesPlayed = Mathf.FloorToInt(startNormalizedTime);
      float currentCycleProgress = startNormalizedTime - cyclesPlayed;

      // Calculate remaining time in current cycle
      float remainingTime = (1.0f - currentCycleProgress) * animationLength;

      Debug.Log($"CharacterInstance: Waiting for animation. StartNormalized: {startNormalizedTime}, CyclesPlayed: {cyclesPlayed}, CurrentCycle: {currentCycleProgress}, Remaining: {remainingTime}");

      // Wait for the current cycle to complete
      if (remainingTime > 0)
      {
        yield return new WaitForSeconds(remainingTime + 0.05f); // Small buffer
      }
      else
      {
        // If we're at the end of a cycle, wait for next cycle to complete
        yield return new WaitForSeconds(animationLength + 0.05f);
      }

      // Wait for state to transition away (back to idle or next state)
      framesWaited = 0;
      while (framesWaited < 30)
      {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.fullPathHash != attackHash)
        {
          Debug.Log($"CharacterInstance: Animation finished - transitioned to new state. Elapsed: {framesWaited} frames");
          break;
        }
        yield return null;
        framesWaited++;
      }

      // Wait one more frame
      yield return null;
    }

    /// <summary>
    /// Waits for a specific animation to finish playing by trigger name.
    /// </summary>
    public System.Collections.IEnumerator WaitForAnimationToFinish(string triggerName)
    {
      if (animator == null)
      {
        yield break;
      }

      // Wait one frame for the animation to start
      yield return null;

      // Get the current state info
      AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

      // Wait until the animation is no longer playing
      // Check if we're in the state matching the trigger name and wait for it to finish
      while (stateInfo.IsName(triggerName) && stateInfo.normalizedTime < 1.0f)
      {
        yield return null;
        stateInfo = animator.GetCurrentAnimatorStateInfo(0);
      }

      // Wait one more frame to ensure transition is complete
      yield return null;
    }

    public void DealDamage(int damage)
    {
      DealDamage(damage, false, false);
    }

    /// <summary>
    /// Deals damage to this character. 
    /// If ignoreArmor is true, damage bypasses armor.
    /// If armorOnly is true, damage only affects armor and never reduces health.
    /// </summary>
    public void DealDamage(int damage, bool ignoreArmor, bool armorOnly)
    {
      if (damage <= 0) return; // No damage to deal

      int originalArmor = currentArmor;
      int originalHealth = currentHealth;

      if (armorOnly)
      {
        // Armor-only damage - only reduce armor, never health
        int armorDamage = Mathf.Min(damage, currentArmor);
        currentArmor -= armorDamage;
        currentArmor = Mathf.Max(0, currentArmor); // Ensure armor doesn't go negative
        Debug.Log($"{GetCharacterName()} takes {damage} armor-only damage! Armor: {originalArmor} -> {currentArmor}, Health unchanged: {currentHealth}");
      }
      else if (ignoreArmor)
      {
        // Ignore armor - apply all damage directly to health
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go negative
        Debug.Log($"{GetCharacterName()} takes {damage} damage (armor ignored)! Health: {originalHealth} -> {currentHealth}");
      }
      else
      {
        // Normal damage - reduce armor first
        int armorDamage = Mathf.Min(damage, currentArmor);
        currentArmor -= armorDamage;
        currentArmor = Mathf.Max(0, currentArmor); // Ensure armor doesn't go negative

        // Calculate leftover damage after armor absorption
        int leftoverDamage = damage - armorDamage;

        // Apply leftover damage to health
        if (leftoverDamage > 0)
        {
          currentHealth -= leftoverDamage;
          currentHealth = Mathf.Max(0, currentHealth); // Ensure health doesn't go negative
        }

        Debug.Log($"{GetCharacterName()} takes {damage} damage! Armor: {originalArmor} -> {currentArmor}, Health: {originalHealth} -> {currentHealth}");
      }
    }

    /// <summary>
    /// Applies skill effects to target and caster. Can be called from anywhere.
    /// </summary>
    public static void ApplySkillEffects(TinySwords2D.Data.Skill skill, CharacterInstance caster, CharacterInstance target)
    {
      if (skill == null)
      {
        Debug.LogWarning("CharacterInstance.ApplySkillEffects: Skill is null!");
        return;
      }

      Debug.Log($"CharacterInstance.ApplySkillEffects: Applying effects of {skill.skillName} from {(caster != null ? caster.GetCharacterName() : "none")} to {(target != null ? target.GetCharacterName() : "none")}");
      Debug.Log($"CharacterInstance.ApplySkillEffects: Target damage: {skill.targetDamage}, Target armor: {skill.targetArmor}, Self damage: {skill.selfDamage}, Self armor: {skill.selfArmor}");

      // Apply target effects
      if (target != null)
      {
        // Apply target damage (negative values = healing)
        if (skill.targetDamage != 0)
        {
          if (skill.targetDamage > 0)
          {
            // Apply damage reduction to the damage dealt (caster's reduction affects their damage output)
            int finalDamage = Mathf.Max(0, skill.targetDamage - caster.damageReduction);
            // Positive damage - check if skill ignores armor or is armor-only
            target.DealDamage(finalDamage, skill.ignoresArmor, skill.armorOnly);
          }
          else
          {
            // Negative damage = healing
            target.currentHealth -= skill.targetDamage; // Subtracting negative = adding
            if (target.Definition != null)
            {
              target.currentHealth = Mathf.Min(target.currentHealth, target.Definition.maxHealth);
            }
            Debug.Log($"{target.GetCharacterName()} heals {Mathf.Abs(skill.targetDamage)} from {skill.skillName}! Health: {target.currentHealth}");
          }
        }

        // Apply target armor changes
        if (skill.targetArmor != 0)
        {
          target.currentArmor += skill.targetArmor;
          target.currentArmor = Mathf.Max(0, target.currentArmor); // Armor can't go negative
          Debug.Log($"{skill.skillName} applies {skill.targetArmor} armor change to {target.GetCharacterName()}. Armor: {target.currentArmor}");
        }

        // Apply damage reduction (flat amount)
        if (skill.damageReduction > 0)
        {
          target.damageReduction += skill.damageReduction;
          Debug.Log($"{skill.skillName} reduces {target.GetCharacterName()}'s damage by {skill.damageReduction}! Total reduction: {target.damageReduction}");

          // If target is an enemy with a planned move, update their attack plan display
          if (target.gameObject.CompareTag("Enemy"))
          {
            EnemyAI enemyAI = target.GetComponent<EnemyAI>();
            if (enemyAI != null && enemyAI.HasPlannedMove())
            {
              enemyAI.UpdateAttackPlanDisplay();
            }
          }
        }
      }

      // Apply self effects to caster
      if (caster != null)
      {
        // Apply self damage (negative values = healing)
        if (skill.selfDamage != 0)
        {
          if (skill.selfDamage > 0)
          {
            // Positive damage - use DealDamage to handle armor
            caster.DealDamage(skill.selfDamage);
          }
          else
          {
            // Negative damage = healing
            caster.currentHealth -= skill.selfDamage; // Subtracting negative = adding
            if (caster.Definition != null)
            {
              caster.currentHealth = Mathf.Min(caster.currentHealth, caster.Definition.maxHealth);
            }
            Debug.Log($"{caster.GetCharacterName()} heals {Mathf.Abs(skill.selfDamage)} from {skill.skillName}! Health: {caster.currentHealth}");
          }
        }

        // Apply self armor changes
        if (skill.selfArmor != 0)
        {
          caster.currentArmor += skill.selfArmor;
          caster.currentArmor = Mathf.Max(0, caster.currentArmor); // Armor can't go negative
          Debug.Log($"{skill.skillName} applies {skill.selfArmor} self armor change to {caster.GetCharacterName()}. Armor: {caster.currentArmor}");
        }
      }

      // Cancel enemy action if skill has this property
      if (skill.cancelsAction && target != null && target.gameObject.CompareTag("Enemy"))
      {
        EnemyAI enemyAI = target.GetComponent<EnemyAI>();
        if (enemyAI != null && enemyAI.HasPlannedMove())
        {
          enemyAI.ClearPlannedMove();
          Debug.Log($"{skill.skillName} cancels {target.GetCharacterName()}'s planned action!");
        }
      }

      // Intercept/taunt: Force enemy to attack only the caster
      if (skill.interceptsAttack && caster != null)
      {
        // Handle single target
        if (target != null && target.gameObject.CompareTag("Enemy"))
        {
          EnemyAI enemyAI = target.GetComponent<EnemyAI>();
          if (enemyAI != null && enemyAI.HasPlannedMove())
          {
            EnemyAI.PlannedMove plannedMove = enemyAI.GetPlannedMove();

            // Only intercept if the planned move is an attack on enemies (not self-buffs or ally buffs)
            if (plannedMove.skill != null &&
                (plannedMove.skill.targetType == SkillTargetType.Enemy ||
                 plannedMove.skill.targetType == SkillTargetType.AllEnemies))
            {
              enemyAI.ChangePlannedMoveTarget(caster);
              Debug.Log($"{skill.skillName} forces {target.GetCharacterName()} to attack {caster.GetCharacterName()}!");
            }
          }
        }
        // Handle AllEnemies target type
        else if (skill.targetType == SkillTargetType.AllEnemies)
        {
          CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
          foreach (CharacterInstance character in allCharacters)
          {
            if (character.gameObject.CompareTag("Enemy") && !character.isDead)
            {
              EnemyAI enemyAI = character.GetComponent<EnemyAI>();
              if (enemyAI != null && enemyAI.HasPlannedMove())
              {
                EnemyAI.PlannedMove plannedMove = enemyAI.GetPlannedMove();

                // Only intercept if the planned move is an attack on enemies (not self-buffs or ally buffs)
                if (plannedMove.skill != null &&
                    (plannedMove.skill.targetType == SkillTargetType.Enemy ||
                     plannedMove.skill.targetType == SkillTargetType.AllEnemies))
                {
                  enemyAI.ChangePlannedMoveTarget(caster);
                  Debug.Log($"{skill.skillName} forces {character.GetCharacterName()} to attack {caster.GetCharacterName()}!");
                }
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Debug method: Triggers death animation. Right-click component in Inspector to use.
    /// </summary>
    [ContextMenu("Debug: Trigger Death Animation")]
    public void DebugTriggerDeath()
    {
      if (animator != null)
      {
        animator.SetTrigger("isDead");
        Debug.Log($"{GetCharacterName()} - Debug: Death animation triggered");
      }
      else
      {
        Debug.LogWarning($"{GetCharacterName()} - Debug: No animator found!");
      }
    }

    /// <summary>
    /// Initializes stamina for all equipped skills to their full requirement (100%).
    /// </summary>
    private void InitializeSkillStamina()
    {
      if (characterDefinition == null) return;

      skillStamina.Clear();
      foreach (Skill skill in characterDefinition.equippedSkills)
      {
        if (skill != null)
        {
          skillStamina[skill] = skill.staminaRequirement;
        }
      }
    }

    /// <summary>
    /// Gets the current stamina for a skill.
    /// </summary>
    public int GetSkillStamina(Skill skill)
    {
      if (skill == null || !skillStamina.ContainsKey(skill))
      {
        return 0;
      }
      return skillStamina[skill];
    }

    /// <summary>
    /// Checks if a skill has enough stamina to be used.
    /// </summary>
    public bool HasEnoughStamina(Skill skill)
    {
      if (skill == null) return false;
      return GetSkillStamina(skill) >= skill.staminaRequirement;
    }

    /// <summary>
    /// Consumes stamina when a skill is used (sets to 0).
    /// </summary>
    public void ConsumeSkillStamina(Skill skill)
    {
      if (skill == null) return;

      if (skillStamina.ContainsKey(skill))
      {
        skillStamina[skill] = 0;
        Debug.Log($"{GetCharacterName()}: Used {skill.skillName}, stamina now: 0/{skill.staminaRequirement}");
      }
    }

    /// <summary>
    /// Adds stamina to a skill (used during round end distribution).
    /// </summary>
    public void AddSkillStamina(Skill skill, int amount)
    {
      if (skill == null || !skillStamina.ContainsKey(skill)) return;

      int currentStamina = skillStamina[skill];
      int maxStamina = skill.staminaRequirement;
      skillStamina[skill] = Mathf.Min(currentStamina + amount, maxStamina);
    }

    // Hover effect methods
    public void OnPointerEnter(PointerEventData eventData)
    {

      if (hasActedThisTurn) return;

      if (spriteRenderer == null)
      {
        Debug.LogWarning($"CharacterInstance: SpriteRenderer is null on {gameObject.name}!");
        return;
      }

      if (hoverSprite != null)
      {
        spriteRenderer.sprite = hoverSprite;
      }
      else if (useBrightnessFallback)
      {
        Color brightColor = originalColor * hoverBrightness;
        brightColor.a = originalColor.a;
        spriteRenderer.color = brightColor;
      }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      if (hasActedThisTurn) return;

      if (spriteRenderer == null) return;

      if (hoverSprite != null && originalSprite != null)
      {
        spriteRenderer.sprite = originalSprite;
      }

      if (useBrightnessFallback)
      {
        spriteRenderer.color = originalColor;
      }
    }
  }
}
