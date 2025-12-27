using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TinySwords2D.Data;
using TinySwords2D.Gameplay;
using TMPro;
using UnityEngine.InputSystem;

namespace TinySwords2D.UI
{
    public class SkillBarController : MonoBehaviour
    {
        public static SkillBarController Instance { get; private set; }

        [Header("Character Source (Choose One)")]
        [Tooltip("Option 1: Use CharacterRoster for multi-character support")]
        [SerializeField] private CharacterRoster roster;

        [Header("UI References")]
        [SerializeField] private Transform buttonParent; // Should be "Background" GameObject

        [Header("Character Name Text")]
        [SerializeField] private GameObject CharacterNameText;

        [Header("Character Avatars")]
        [Tooltip("Avatar GameObject for Character 1")]
        [SerializeField] private GameObject avatar1;

        [Tooltip("Avatar GameObject for Character 2")]
        [SerializeField] private GameObject avatar2;

        [Tooltip("Avatar GameObject for Character 3")]
        [SerializeField] private GameObject avatar3;

        [Header("Character Avatar Mappings")]
        [Tooltip("Character Definition that corresponds to Avatar 1")]
        [SerializeField] private CharacterDefinition character1;

        [Tooltip("Character Definition that corresponds to Avatar 2")]
        [SerializeField] private CharacterDefinition character2;

        [Tooltip("Character Definition that corresponds to Avatar 3")]
        [SerializeField] private CharacterDefinition character3;

        [Header("Targeting")]
        [SerializeField] private TargetingManager targetingManager;

        [Header("Button Sprites")]
        [Tooltip("Button frame sprites with hotkey numbers. Index 0 = hotkey 1, Index 1 = hotkey 2, etc.")]
        [SerializeField] private Sprite[] buttonUnlockedSprites = new Sprite[10];
        [SerializeField] private Sprite[] buttonLockedSprites = new Sprite[10];

        public bool isExecutingSkill = false;

        public Dictionary<int, SkillButtonUI> ButtonPool => buttonPool;

        private Dictionary<int, SkillButtonUI> buttonPool = new Dictionary<int, SkillButtonUI>();

        [SerializeField] private SkillButtonUI skillButtonPrefab;

        private const int maxSkillSlots = 10;

        #region Lifecycle Events
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // Throw error if button parent is not assigned
            if (buttonParent == null)
            {
                Debug.LogError("SkillBarController: ButtonParent is NULL");
            }

            Debug.Log("SkillBarController: Awake called");
            // Subscribe to roster events if using roster
            if (roster != null)
            {
                // Remove listener first to avoid duplicates
                roster.OnActiveCharacterChanged.RemoveListener(HandleCharacterChanged);
                // Add listener
                roster.OnActiveCharacterChanged.AddListener(HandleCharacterChanged);
            }
        }

        private void OnEnable()
        {
            // Subscribe to turn changes
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from turn changes
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }

            if (roster != null)
            {
                roster.OnActiveCharacterChanged.RemoveListener(HandleCharacterChanged);
            }
        }

        private void Destroy()
        {
            if (roster != null)
            {
                roster.OnActiveCharacterChanged.RemoveListener(HandleCharacterChanged);
            }
        }
        #endregion

        private void HandleCharacterChanged(CharacterDefinition character)
        {
            Debug.Log($"SkillBarController: HandleCharacterChanged called with: {character?.characterName ?? "NULL"}");

            // Update character name display
            CharacterNameText.GetComponent<TextMeshProUGUI>().text = character.characterName;

            // Show/hide avatars based on active character
            UpdateAvatarVisibility(character);

            // Create a fresh button pool for the new character
            InitializeButtonPool();

            var skills = character.equippedSkills;
            CharacterInstance characterInstance = roster.ActiveCharacterInstance;

            for (int i = 1; i <= maxSkillSlots; i++)
            {
                bool hasSkill = (i - 1) < skills.Count;

                if (hasSkill && buttonPool.ContainsKey(i))
                {
                    SkillButtonUI buttonUI = buttonPool[i];
                    Skill skill = skills[i - 1];
                    buttonUI.BindSkill(skill, i);

                    // Lock skill if it doesn't have enough stamina
                    bool hasEnoughStamina = characterInstance != null && characterInstance.HasEnoughStamina(skill);
                    buttonUI.ToggleLock(!hasEnoughStamina);
                }
            }
        }

        /// <summary>
        /// Updates avatar visibility based on the active character
        /// </summary>
        private void UpdateAvatarVisibility(CharacterDefinition activeCharacter)
        {
            if (activeCharacter == null)
            {
                Debug.LogWarning("SkillBarController: Active character is null, cannot update avatars");
                return;
            }

            // Hide all avatars first
            SetAvatarVisibility(avatar1, false);
            SetAvatarVisibility(avatar2, false);
            SetAvatarVisibility(avatar3, false);

            // Show the avatar that matches the active character
            if (activeCharacter == character1)
            {
                SetAvatarVisibility(avatar1, true);
                Debug.Log($"SkillBarController: Showing avatar 1 for {activeCharacter.characterName}");
            }
            else if (activeCharacter == character2)
            {
                SetAvatarVisibility(avatar2, true);
                Debug.Log($"SkillBarController: Showing avatar 2 for {activeCharacter.characterName}");
            }
            else if (activeCharacter == character3)
            {
                SetAvatarVisibility(avatar3, true);
                Debug.Log($"SkillBarController: Showing avatar 3 for {activeCharacter.characterName}");
            }
            else
            {
                Debug.LogWarning($"SkillBarController: No avatar mapping found for {activeCharacter.characterName}");
            }
        }

        /// <summary>
        /// Helper method to set avatar visibility
        /// </summary>
        private void SetAvatarVisibility(GameObject avatar, bool visible)
        {
            if (avatar != null)
            {
                avatar.SetActive(visible);
            }
        }

        #region Button Pool
        private void InitializeButtonPool()
        {

            if (buttonParent == null)
            {
                Debug.LogError("SkillBarController: Cannot find button parent! Please assign Background GameObject.");
                return;
            }

            // Clear existing pool
            DestroyButtonPool();

            // Find all existing SkillButton GameObjects (SkillButton1-9, 0)
            // Always find all 10 buttons regardless of how many skills exist
            for (int i = 1; i <= maxSkillSlots; i++)
            {
                string buttonName = GetButtonName(i);
                SkillButtonUI buttonUI = Instantiate(skillButtonPrefab, buttonParent);
                buttonUI.gameObject.name = buttonName;
                buttonUI.hotkeyNumber = i == 10 ? 0 : i;

                var unlockedFrame = buttonUI.unlockedFrame.gameObject.GetComponent<Image>();
                var lockedFrame = buttonUI.lockedFrame.gameObject.GetComponent<Image>();

                Sprite newUnlockedFrameSprite = buttonUnlockedSprites[(i - 1)];
                Sprite newLockedFrameSprite = buttonLockedSprites[(i - 1)];

                unlockedFrame.sprite = newUnlockedFrameSprite;
                lockedFrame.sprite = newLockedFrameSprite;

                buttonUI.ToggleLock(true);

                buttonPool.Add(i, buttonUI);
                buttonUI.OnSkillInvoked.AddListener(HandleCrossButtonClicked);
            }
        }


        private void HandleCrossButtonClicked(SkillButtonUI buttonUI = null)
        {
            // Cancel targeting if active (when clicking any skill button)
            if (targetingManager != null && targetingManager.IsTargeting())
            {
                targetingManager.CancelTargeting();
            }

            // Loop through buttonPool and unset others
            foreach (var button in buttonPool)
            {
                if (button.Value != buttonUI)
                {
                    button.Value.UpdateSelectedState(false);
                }
            }

            // Unsubscribe from events
            if (targetingManager != null)
            {
                targetingManager.OnTargetSelected -= ExecuteSkillOnTarget;
            }

            if (buttonUI != null)
            {
                HandleSkillClicked(buttonUI);
            }
        }

        private string GetButtonName(int i)
        {
            return i == 10 ? "SkillButton0" : $"SkillButton{i}";
        }

        private void DestroyButtonPool()
        {
            if (buttonParent == null)
            {
                Debug.LogError("SkillBarController: Cannot find button parent! Please assign Background GameObject.");
                return;
            }

            // Remove cross button listeners
            foreach (var button in buttonPool)
            {
                button.Value.OnSkillInvoked.RemoveListener(HandleCrossButtonClicked);

            }

            // Clear existing pool
            buttonPool.Clear();

            // Find all existing SkillButton GameObjects (SkillButton1-9, 0)
            // Always find all 10 buttons regardless of how many skills exist
            for (int i = 1; i <= maxSkillSlots; i++)
            {
                string buttonName = i == 10 ? "SkillButton0" : $"SkillButton{i}";
                Transform buttonTransform = buttonParent.Find(buttonName);

                SkillButtonUI buttonUI = buttonTransform.GetComponent<SkillButtonUI>();

                Destroy(buttonUI.gameObject);
            }
        }
        #endregion

        // Pre-execution checks for a skill
        private void HandleSkillClicked(SkillButtonUI skillButton)
        {
            Skill skill = skillButton.BoundSkill;

            // Prevent clicking skills while one is executing
            if (isExecutingSkill)
            {
                return;
            }

            if (skill == null)
            {
                return;
            }

            // Find the active character instance
            CharacterInstance activeCharacter = roster.ActiveCharacterInstance;

            if (activeCharacter == null)
            {
                return;
            }

            // Check if character has already acted this turn
            if (activeCharacter.hasActedThisTurn)
            {
                return;
            }

            // Check if skill has enough stamina
            if (!activeCharacter.HasEnoughStamina(skill))
            {
                Debug.Log($"SkillBarController: {skill.skillName} doesn't have enough stamina!");
                return;
            }

            // Check if it's player turn
            if (TurnManager.Instance != null && TurnManager.Instance.CurrentTurn != TurnManager.TurnState.PlayerTurn)
            {
                return;
            }

            // Check if skill requires targeting
            if (skill.targetType == SkillTargetType.Enemy || skill.targetType == SkillTargetType.Ally)
            {
                // Start targeting mode - DON'T play animation yet
                if (targetingManager == null)
                {
                    Debug.LogWarning("SkillBarController: No TargetingManager found! Cannot target enemies.");
                    return;
                }

                targetingManager.StartTargeting(skillButton, activeCharacter);

                // Subscribe to target selection event
                targetingManager.OnTargetSelected += ExecuteSkillOnTarget;

                // Don't play animation here - wait for target selection
                return;
            }
            else
            {
                // No targeting needed - execute immediately
                ExecuteSkill(skillButton, activeCharacter, null);
            }
        }

        private void ExecuteSkillOnTarget(SkillButtonUI skillButton, CharacterInstance caster, CharacterInstance target)
        {
            Skill skill = skillButton.BoundSkill;

            Debug.Log($"SkillBarController: ExecuteSkillOnTarget called with skill: {skill.skillName}, caster: {caster.GetCharacterName()}, target: {target.GetCharacterName()}");
            // Unsubscribe from events
            if (targetingManager != null)
            {
                targetingManager.OnTargetSelected -= ExecuteSkillOnTarget;
            }

            // Now execute the skill with the selected target
            ExecuteSkill(skillButton, caster, target);
        }

        // Executes a skill
        private void ExecuteSkill(SkillButtonUI skillButton, CharacterInstance caster, CharacterInstance target)
        {
            Skill skill = skillButton.BoundSkill;

            // Cancel targeting if active
            if (targetingManager != null && targetingManager.IsTargeting())
            {
                targetingManager.CancelTargeting();
            }

            // Prevent multiple executions
            if (isExecutingSkill)
            {
                Debug.LogWarning($"SkillBarController: Attempted to execute {skill.skillName} while another skill is executing!");
                return;
            }

            // Set flag to prevent re-execution
            isExecutingSkill = true;

            StartCoroutine(ExecuteSkillCoroutine(skillButton, caster, target));
        }

        #region Public Methods
        public System.Collections.IEnumerator ExecuteSkillCoroutine(Skill skill, CharacterInstance caster, CharacterInstance target)
        {
            return ExecuteSkillCoroutine(skill, null, caster, target);
        }
        public System.Collections.IEnumerator ExecuteSkillCoroutine(SkillButtonUI skillButton, CharacterInstance caster, CharacterInstance target)
        {
            return ExecuteSkillCoroutine(null, skillButton, caster, target);
        }
        #endregion

        #region Execute Skill Coroutine
        private System.Collections.IEnumerator ExecuteSkillCoroutine(Skill skill, SkillButtonUI skillButton, CharacterInstance caster, CharacterInstance target)
        {
            // If skill button is provided, use the skill from the button
            if (skillButton != null)
            {
                skill = skillButton.BoundSkill;
            }

            try
            {
                // Consume stamina when skill is used
                caster.ConsumeSkillStamina(skill);

                // Play the animation
                caster.PlaySkillAnimation(skill);


                // Check if character uses a projectile
                CharacterDefinition characterDef = caster?.Definition;
                GameObject projectilePrefab = characterDef != null ? characterDef.GetProjectilePrefab() : null;

                // Handle AllEnemies target type - apply to all enemies
                if (skill.targetType == SkillTargetType.AllEnemies)
                {
                    // Wait for animation to finish
                    yield return null;
                    yield return caster.WaitForCurrentAnimation();

                    // Find all enemies and apply skill effects to each
                    CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
                    foreach (CharacterInstance enemy in allCharacters)
                    {
                        if (enemy != null && enemy.gameObject.CompareTag("Enemy") && !enemy.isDead)
                        {
                            Debug.Log($"{caster.GetCharacterName()} uses {skill.skillName} on {enemy.GetCharacterName()}! (AllEnemies)");
                            CharacterInstance.ApplySkillEffects(skill, caster, enemy);
                        }
                    }
                }
                // Handle AllAllies target type - apply to all allies
                else if (skill.targetType == SkillTargetType.AllAllies)
                {
                    // Wait for animation to finish
                    yield return null;
                    yield return caster.WaitForCurrentAnimation();

                    // Find all allies and apply skill effects to each
                    CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
                    foreach (CharacterInstance ally in allCharacters)
                    {
                        if (ally != null && ally.gameObject.CompareTag("Player") && !ally.isDead)
                        {
                            Debug.Log($"{caster.GetCharacterName()} uses {skill.skillName} on {ally.GetCharacterName()}! (AllAllies)");
                            CharacterInstance.ApplySkillEffects(skill, caster, ally);
                        }
                    }
                }
                // Only use projectile for Ranged skills with a single target
                else if (skill.skillType == SkillType.Ranged && projectilePrefab != null && target != null)
                {
                    Debug.Log($"SkillBarController: Executing skill '{skill.skillName}' with projectile '{projectilePrefab.name}'");
                    // Spawn projectile and wait for it to hit
                    yield return StartCoroutine(SpawnProjectileAndWaitForHit(skill, caster, target));
                }
                else
                {
                    // Melee/non-projectile - wait for animation to finish
                    // Wait a frame for the trigger to take effect
                    yield return null;

                    // Now wait for the animation to finish
                    yield return caster.WaitForCurrentAnimation();

                    // Apply skill effects AFTER animation finishes
                    // For self-target skills, pass caster as target so self effects apply
                    CharacterInstance actualTarget = target ?? caster;

                    if (actualTarget != null)
                    {
                        if (target != null)
                        {
                            Debug.Log($"{caster.GetCharacterName()} uses {skill.skillName} on {target.GetCharacterName()}! (Melee)");
                        }
                        else
                        {
                            Debug.Log($"{caster.GetCharacterName()} uses {skill.skillName} on themselves!");
                        }
                        CharacterInstance.ApplySkillEffects(skill, caster, actualTarget);
                    }
                }

                // Wait a moment for visual effects
                yield return new WaitForSeconds(0.2f);

                // Call additional effects (like ending turn)
                OnSkillExecutionComplete(skillButton, caster, target);
            }
            finally
            {
                // Always clear the flag, even if there's an error
                isExecutingSkill = false;
            }
        }

        private System.Collections.IEnumerator SpawnProjectileAndWaitForHit(Skill skill, CharacterInstance caster, CharacterInstance target)
        {
            // Wait for spawn delay
            float spawnDelay = caster.Definition.GetProjectileSpawnDelay();
            yield return new WaitForSeconds(spawnDelay);

            // Spawn projectile
            Vector3 spawnPos = caster.GetProjectileSpawnPosition();
            Vector3 targetPos = target.transform.position;

            if (spawnPos == targetPos)
            {
                targetPos += Vector3.right * 0.1f;
            }

            GameObject projectilePrefab = caster.Definition.GetProjectilePrefab();
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();

            if (projectile == null)
            {
                projectile = projectileObj.AddComponent<Projectile>();
            }

            float projectileSpeed = caster.Definition.GetProjectileSpeed();
            float arcHeight = caster.Definition.GetProjectileArcHeight();

            // Initialize projectile
            projectile.Initialize(
                spawnPos,
                targetPos,
                projectileSpeed,
                skill,
                caster,
                target
            );
            projectile.SetArcHeight(arcHeight);

            // Wait for projectile to be destroyed (hits target or times out)
            // Note: Skill effects are now applied by the projectile when it hits
            yield return new WaitUntil(() => projectile == null || projectile.gameObject == null);
        }

        private void OnSkillExecutionComplete(SkillButtonUI skillButton, CharacterInstance caster, CharacterInstance target)
        {
            // Clear selected skill after execution (only if skillButton exists)
            if (skillButton != null)
            {
                skillButton.UpdateSelectedState(false);
            }

            // Mark character as having acted (only for player characters)
            if (caster != null && caster.gameObject.CompareTag("Player") && TurnManager.Instance != null)
            {
                bool allActed = TurnManager.Instance.MarkCharacterAsActed(caster);

                if (allActed)
                {
                    // All characters have acted, end player turn
                    TurnManager.Instance.EndPlayerTurn();
                }
                else
                {
                    roster.NextActiveCharacter();
                }
            }
        }
        #endregion

        private void Update()
        {
            // Hide skill buttons during enemy turn
            bool isPlayerTurn = TurnManager.Instance != null &&
                        TurnManager.Instance.CurrentTurn == TurnManager.TurnState.PlayerTurn;

            foreach (var button in buttonPool.Values)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(isPlayerTurn);
                }
            }
        }

        // Add this method to handle turn changes
        private void HandleTurnChanged(TurnManager.TurnState newTurn)
        {
            // Execute DefaultControllerState when enemy turn begins
            if (newTurn == TurnManager.TurnState.EnemyTurn)
            {
                DefaultControllerState(newTurn);
            }
        }

        private void DefaultControllerState(TurnManager.TurnState newTurn)
        {
            InitializeButtonPool();
        }

        // Add a method to update skill locks based on stamina
        public void UpdateSkillLocks()
        {
            CharacterInstance activeCharacter = roster.ActiveCharacterInstance;
            if (activeCharacter == null) return;

            foreach (var kvp in buttonPool)
            {
                SkillButtonUI buttonUI = kvp.Value;
                Skill skill = buttonUI.BoundSkill;

                if (skill != null)
                {
                    bool hasEnoughStamina = activeCharacter.HasEnoughStamina(skill);
                    buttonUI.ToggleLock(!hasEnoughStamina);
                }
            }
        }
    }
}

