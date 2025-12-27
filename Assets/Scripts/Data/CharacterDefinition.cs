using System.Collections.Generic;
using UnityEngine;

namespace TinySwords2D.Data
{
    [System.Serializable]
    public class SkillAnimationOverride
    {
        public Skill skill;
        public string animationTrigger;
    }

    [CreateAssetMenu(menuName = "Tiny Swords/Character Definition", fileName = "NewCharacter")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string characterName = "New Hero";
        public Sprite portrait;

        [Header("Stats")]
        public int maxHealth = 100;

        [Header("Skill Loadout")]
        public List<Skill> equippedSkills = new List<Skill>();

        [Header("Animation Overrides")]
        [Tooltip("Override animation triggers for specific skills. Leave empty to use skill's default animation.")]
        public List<SkillAnimationOverride> animationOverrides = new List<SkillAnimationOverride>();

        [Header("Projectile Settings")]
        [Tooltip("Projectile prefab to spawn for this character. Leave null if character doesn't use projectiles.")]
        public GameObject projectilePrefab;
        [Tooltip("Default projectile speed for this character.")]
        public float defaultProjectileSpeed = 10f;
        [Tooltip("Default arc height for projectiles.")]
        public float defaultProjectileArcHeight = 2f;
        [Tooltip("Delay before spawning projectile (to sync with animation).")]
        public float defaultProjectileSpawnDelay = 0.2f;

        /// <summary>
        /// Gets the animation trigger for a skill, using override if available, otherwise skill's default.
        /// </summary>
        public string GetAnimationTriggerForSkill(Skill skill)
        {
            if (skill == null) return string.Empty;

            // Check for override
            foreach (var overrideEntry in animationOverrides)
            {
                if (overrideEntry.skill == skill)
                {
                    return overrideEntry.animationTrigger;
                }
            }

            // Use skill's default
            return skill.animationTrigger;
        }

        /// <summary>
        /// Gets the projectile prefab for this character.
        /// </summary>
        public GameObject GetProjectilePrefab()
        {
            return projectilePrefab;
        }

        /// <summary>
        /// Gets projectile speed for this character.
        /// </summary>
        public float GetProjectileSpeed()
        {
            return defaultProjectileSpeed;
        }

        /// <summary>
        /// Gets projectile arc height for this character.
        /// </summary>
        public float GetProjectileArcHeight()
        {
            return defaultProjectileArcHeight;
        }

        /// <summary>
        /// Gets projectile spawn delay for this character.
        /// </summary>
        public float GetProjectileSpawnDelay()
        {
            return defaultProjectileSpawnDelay;
        }
    }
}

