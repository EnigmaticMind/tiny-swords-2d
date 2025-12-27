using UnityEngine;
using TinySwords2D.Gameplay;
using TinySwords2D.Data;

namespace TinySwords2D.Gameplay
{
    /// <summary>
    /// Handles projectile movement and collision.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float arcHeight = 2f; // Height of the arc

        [Header("Trail Settings")]
        [SerializeField] private bool useTrail = true;
        [SerializeField] private float trailTime = 0.5f;
        [SerializeField] private float trailWidth = 0.1f;
        [SerializeField] private Color trailColor = Color.white;

        private Vector3 startPosition;
        private Vector3 targetPosition;
        private CharacterInstance target;
        private CharacterInstance caster;
        private Skill skill;
        private bool hasTarget = false;
        private float spawnTime;
        private float journeyLength;
        private float journeyTime;
        private TrailRenderer trailRenderer;
        private bool isInitialized = false; // Add this flag

        public void Initialize(Vector3 startPos, Vector3 endPos, float projectileSpeed, Skill projectileSkill, CharacterInstance projectileCaster, CharacterInstance projectileTarget = null)
        {
            Debug.Log($"Projectile Initialize: prefabRootRotation={Quaternion.identity}");
            startPosition = startPos;
            targetPosition = endPos;
            speed = projectileSpeed;
            skill = projectileSkill;
            caster = projectileCaster;
            target = projectileTarget;
            hasTarget = target != null;
            spawnTime = Time.time; // Set spawn time

            // Calculate journey parameters with safety checks
            journeyLength = Vector3.Distance(startPosition, targetPosition);

            // Safety check: prevent division by zero
            if (speed <= 0)
            {
                Debug.LogError($"Projectile: Invalid speed {speed}, using default 10");
                speed = 10f;
            }

            if (journeyLength <= 0)
            {
                Debug.LogWarning("Projectile: Start and end positions are the same!");
                journeyLength = 0.1f; // Small value to prevent issues
            }

            journeyTime = journeyLength / speed;

            // Ensure journeyTime is valid
            if (journeyTime <= 0 || float.IsInfinity(journeyTime) || float.IsNaN(journeyTime))
            {
                Debug.LogError($"Projectile: Invalid journeyTime {journeyTime}, recalculating");
                journeyTime = 1f; // Default to 1 second
            }

            // Set sorting order so projectile appears in front of characters
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                // Try to find SpriteRenderer on child objects (in case sprite is a child)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                // Set sorting layer to "Particles" and order to 2
                spriteRenderer.sortingLayerName = "Particles";
            }

            // Setup trail renderer BEFORE any rotation changes
            SetupTrailRenderer();

            // Rotate to face the target
            Vector3 initialDirection = (endPos - startPos).normalized;
            if (initialDirection != Vector3.zero)
            {
                float angle = Mathf.Atan2(initialDirection.y, initialDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }

            // Set initial position - IMPORTANT: Do this last
            transform.position = startPosition;

            // Mark as initialized
            isInitialized = true;

            Debug.Log($"Projectile initialized: Final root rotation={transform.rotation.eulerAngles}");
        }

        private void Start()
        {
            // Don't log error if not initialized yet - Initialize() might be called after Start()
            if (isInitialized)
            {
                Debug.Log($"Projectile Start: Initialized correctly. JourneyTime={journeyTime}, Start={startPosition}, Target={targetPosition}");
            }
        }

        private void Update()
        {
            // Early exit if not initialized
            if (!isInitialized)
            {
                return; // Wait for Initialize() to be called
            }

            // Early exit if journeyTime is invalid
            if (journeyTime <= 0 || float.IsInfinity(journeyTime) || float.IsNaN(journeyTime))
            {
                Debug.LogError($"Projectile: Invalid journeyTime {journeyTime}, destroying projectile");
                Destroy(gameObject);
                return;
            }

            // Check lifetime
            if (Time.time - spawnTime > lifetime)
            {
                Debug.Log("Projectile: Lifetime exceeded, destroying");
                Destroy(gameObject);
                return;
            }

            // Update target position if we have a moving target
            if (hasTarget && target != null)
            {
                Vector3 newTargetPosition = target.transform.position;

                // Only recalculate if target moved significantly from ORIGINAL target position
                // Don't use current transform.position for comparison - use the original start position
                float distanceFromStart = Vector3.Distance(startPosition, newTargetPosition);
                float originalDistance = Vector3.Distance(startPosition, targetPosition);

                // Update target position
                targetPosition = newTargetPosition;

                // Only recalculate if the NEW distance is significantly different from the ORIGINAL distance
                // This prevents constant recalculation
                if (Mathf.Abs(distanceFromStart - originalDistance) > journeyLength * 0.2f)
                {
                    // Target moved significantly - recalculate trajectory
                    startPosition = transform.position;
                    journeyLength = Vector3.Distance(startPosition, targetPosition);

                    if (journeyLength > 0 && speed > 0)
                    {
                        journeyTime = journeyLength / speed;
                        spawnTime = Time.time; // Reset timer for new trajectory
                        Debug.Log($"Projectile: Recalculated trajectory. New journeyTime={journeyTime}");
                    }
                }
            }

            // Calculate progress along the arc (0 to 1)
            float elapsedTime = Time.time - spawnTime;
            float progress = elapsedTime / journeyTime;

            if (progress >= 1.0f)
            {
                // Reached target
                transform.position = targetPosition;
                Debug.Log("Projectile: Reached target");
                OnReachedTarget();
                return;
            }

            // Calculate arc position
            Vector3 currentPosition = CalculateArcPosition(progress);

            // Store previous position for comparison
            Vector3 previousPos = transform.position;

            // Update position
            transform.position = currentPosition;

            // Check if position is actually changing
            float positionChange = Vector3.Distance(previousPos, currentPosition);
            if (positionChange < 0.0001f && progress > 0.01f)
            {
                Debug.LogWarning($"Projectile: Position not changing! Prev={previousPos}, New={currentPosition}, Progress={progress}, Change={positionChange}");
            }

            // Always rotate projectile to face movement direction
            Vector3 direction = CalculateArcDirection(progress);
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        private void SetupTrailRenderer()
        {
            if (!useTrail) return;

            // Check if TrailPoint already exists in the prefab (search recursively)
            Transform trailTransform = FindChildRecursive(transform, "TrailPoint");

            // Only set up trail if TrailPoint already exists - don't create one
            if (trailTransform == null)
            {
                Debug.Log($"Projectile: No TrailPoint found, skipping trail setup");
                return;
            }

            Debug.Log($"Projectile: Using existing TrailPoint at local position {trailTransform.localPosition}, world position {trailTransform.position}");

            // Put TrailRenderer on the TrailPoint child GameObject
            // Since TrailPoint is a child of the projectile, it will move with it
            trailRenderer = trailTransform.GetComponent<TrailRenderer>();

            if (trailRenderer == null)
            {
                // Add TrailRenderer component to the TrailPoint child
                trailRenderer = trailTransform.gameObject.AddComponent<TrailRenderer>();
            }

            // Configure trail renderer
            trailRenderer.time = trailTime;
            trailRenderer.startWidth = trailWidth;
            trailRenderer.endWidth = trailWidth * 0.1f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.colorGradient = CreateTrailGradient();
            trailRenderer.alignment = LineAlignment.TransformZ;
        }

        private Gradient CreateTrailGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(trailColor, 0.0f),
                    new GradientColorKey(trailColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            return gradient;
        }

        private Vector3 CalculateArcPosition(float progress)
        {
            // Linear interpolation between start and end
            Vector3 linearPos = Vector3.Lerp(startPosition, targetPosition, progress);

            // Add arc height (parabolic curve)
            float arc = arcHeight * (progress - progress * progress) * 4f;
            // This creates a smooth arc: starts at 0, peaks at progress = 0.5, ends at 0

            return new Vector3(linearPos.x, linearPos.y + arc, linearPos.z);
        }

        private Vector3 CalculateArcDirection(float progress)
        {
            // Calculate direction by looking at a point slightly ahead
            float nextProgress = Mathf.Min(progress + 0.01f, 1.0f);
            Vector3 currentPos = CalculateArcPosition(progress);
            Vector3 nextPos = CalculateArcPosition(nextProgress);
            return (nextPos - currentPos).normalized;
        }

        private void OnReachedTarget()
        {
            // Apply skill effects if we have a target and skill
            if (target != null && skill != null)
            {
                CharacterInstance.ApplySkillEffects(skill, caster, target);
            }
            else if (target != null)
            {
                Debug.LogWarning($"Projectile hit {target.GetCharacterName()} but no skill assigned!");
            }

            // TODO: Play hit effect, sound, etc.
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"Projectile OnTriggerEnter2D: Hit {other.gameObject.name}, Tag={other.tag}, IsTrigger={other.isTrigger}");

            // Check if we hit the target
            if (target != null)
            {
                CharacterInstance hitCharacter = other.GetComponent<CharacterInstance>();
                if (hitCharacter == target)
                {
                    Debug.Log($"Projectile: Hit target {target.GetCharacterName()}");
                    OnReachedTarget();
                    return;
                }
            }
            else
            {
                // If no specific target, check if we hit any enemy
                if (other.CompareTag("Enemy"))
                {
                    CharacterInstance hitCharacter = other.GetComponent<CharacterInstance>();
                    if (hitCharacter != null && hitCharacter != caster)
                    {
                        // Update target to the hit character and apply effects
                        target = hitCharacter;
                        if (skill != null)
                        {
                            CharacterInstance.ApplySkillEffects(skill, caster, target);
                        }
                        OnReachedTarget();
                        return;
                    }
                }
            }
        }

        public void SetArcHeight(float height)
        {
            arcHeight = height;
        }

        /// <summary>
        /// Applies skill effects to the target when projectile hits.
        /// </summary>
        private void ApplySkillEffects(Skill skill, CharacterInstance caster, CharacterInstance target)
        {
            if (skill == null || target == null)
            {
                return;
            }

            Debug.Log($"Projectile: Applying {skill.skillName} effects from {caster.GetCharacterName()} to {target.GetCharacterName()}");

            // Apply target damage
            if (skill.targetDamage != 0)
            {
                if (skill.targetDamage > 0)
                {
                    // Apply damage reduction to the damage dealt
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
            }

            // Apply self effects to caster
            if (caster != null)
            {
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

                if (skill.selfArmor != 0)
                {
                    // TODO: Implement armor system
                    Debug.Log($"{skill.skillName} applies {skill.selfArmor} self armor change to {caster.GetCharacterName()}");
                }
            }
        }

        // Helper method to find child recursively
        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}

