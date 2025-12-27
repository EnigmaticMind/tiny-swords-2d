using UnityEngine;
using TinySwords2D.Data;
using TinySwords2D.Gameplay;
using System;
using TinySwords2D.UI;

namespace TinySwords2D.Gameplay
{
    public class TargetingManager : MonoBehaviour
    {
        public static TargetingManager Instance { get; private set; }

        [Header("Targeting Settings")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private bool isTargeting = false;
        [SerializeField] private SkillButtonUI pendingSkillButton;
        [SerializeField] private CharacterInstance caster;

        public event Action<SkillButtonUI, CharacterInstance, CharacterInstance> OnTargetSelected;
        public event Action OnTargetingCancelled;

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
        }

        public void StartTargeting(SkillButtonUI skillButton, CharacterInstance skillCaster)
        {
            if (skillButton == null || skillCaster == null)
            {
                Debug.LogWarning("TargetingManager: Cannot start targeting - skill or caster is null");
                return;
            }

            pendingSkillButton = skillButton;
            caster = skillCaster;
            isTargeting = true;
            Debug.Log($"TargetingManager: Started targeting for skill '{skillButton.BoundSkill.skillName}'");
        }

        public void SelectTarget(CharacterInstance target)
        {
            if (!isTargeting)
            {
                Debug.LogWarning("TargetingManager: Not in targeting mode");
                return;
            }

            if (target == null)
            {
                Debug.LogWarning("TargetingManager: Target is null");
                return;
            }

            // Validate target based on skill target type and caster type
            bool isValidTarget = false;

            if (pendingSkillButton.BoundSkill.targetType == SkillTargetType.Enemy)
            {
                // If caster is a player, target must be an enemy
                // If caster is an enemy, target must be a player
                isValidTarget = IsEnemy(target);
            }
            else if (pendingSkillButton.BoundSkill.targetType == SkillTargetType.Ally)
            {
                // If caster is a player, target must be a player (ally)
                // If caster is an enemy, target must be an enemy (ally)
                isValidTarget = !IsEnemy(target);
            }
            else
            {
                // Self, AllAllies, AllEnemies - no validation needed
                isValidTarget = true;
            }

            if (!isValidTarget)
            {
                bool casterIsEnemy = caster.gameObject.CompareTag("Enemy");
                bool targetIsEnemy = target.gameObject.CompareTag("Enemy");
                Debug.LogWarning($"TargetingManager: Invalid target! Caster is {(casterIsEnemy ? "Enemy" : "Player")}, Target is {(targetIsEnemy ? "Enemy" : "Player")}, Skill target type: {pendingSkillButton.BoundSkill.targetType}");
                return;
            }

            Debug.Log($"TargetingManager: Target selected: {target.GetCharacterName()}");

            OnTargetSelected?.Invoke(pendingSkillButton, caster, target);

            // Reset targeting state
            isTargeting = false;
            pendingSkillButton = null;
            caster = null;
        }

        public void CancelTargeting()
        {
            if (!isTargeting) return;

            Debug.Log("TargetingManager: Targeting cancelled");
            isTargeting = false;
            pendingSkillButton = null;
            caster = null;
            OnTargetingCancelled?.Invoke();
        }

        public bool IsTargeting()
        {
            return isTargeting;
        }

        private bool IsEnemy(CharacterInstance character)
        {
            if (caster.gameObject.CompareTag("Player"))
            {
                return character.gameObject.CompareTag("Enemy");
            }

            return character.gameObject.CompareTag("Player");
        }

        // Add these public getter methods
        public Skill GetPendingSkill()
        {
            return pendingSkillButton?.BoundSkill;
        }

        public CharacterInstance GetCaster()
        {
            return caster;
        }

        private void Update()
        {
            // Cancel targeting with Escape key
            if (isTargeting && UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CancelTargeting();
                }
            }
        }
    }
}
