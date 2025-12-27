using UnityEngine;
using TinySwords2D.Data;
using TinySwords2D.Gameplay;
using System.Linq;

namespace TinySwords2D.Gameplay
{
    /// <summary>
    /// Utility class for target validation logic.
    /// </summary>
    public static class TargetingUtility
    {
        /// <summary>
        /// Gets the lane number (1 or 2) that a character is in, based on parent GameObject name.
        /// Returns 0 if lane cannot be determined.
        /// </summary>
        public static int GetLaneNumber(CharacterInstance character)
        {
            if (character == null) return 0;

            Transform current = character.transform;

            // Check parent
            if (current.parent != null)
            {
                string parentName = current.parent.name;

                // Check for "Lane1", "Lane 1", "lane1", etc.
                if (parentName.Contains("Lane1") || parentName.Contains("lane1") || parentName.Contains("Lane 1"))
                {
                    return 1;
                }

                // Check for "Lane2", "Lane 2", "lane2", etc.
                if (parentName.Contains("Lane2") || parentName.Contains("lane2") || parentName.Contains("Lane 2"))
                {
                    return 2;
                }

                // Check grandparent if parent didn't have lane info
                if (current.parent.parent != null)
                {
                    string grandparentName = current.parent.parent.name;

                    // Check for "Lane1", "Lane 1", "lane1", etc.
                    if (grandparentName.Contains("Lane1") || grandparentName.Contains("lane1") || grandparentName.Contains("Lane 1"))
                    {
                        return 1;
                    }

                    // Check for "Lane2", "Lane 2", "lane2", etc.
                    if (grandparentName.Contains("Lane2") || grandparentName.Contains("lane2") || grandparentName.Contains("Lane 2"))
                    {
                        return 2;
                    }
                }
            }

            return 0; // Unknown lane
        }

        /// <summary>
        /// Checks if a lane is empty (has no characters with the specified tag).
        /// </summary>
        private static bool IsLaneEmpty(int laneNumber, string characterTag)
        {
            if (laneNumber == 0) return true;

            // Find all CharacterInstances
            CharacterInstance[] allCharacters = Object.FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);

            foreach (CharacterInstance character in allCharacters)
            {
                if (character.gameObject.CompareTag(characterTag))
                {
                    int characterLane = GetLaneNumber(character);
                    if (characterLane == laneNumber)
                    {
                        return false; // Found a character in this lane
                    }
                }
            }

            return true; // Lane is empty
        }

        /// <summary>
        /// Checks if a character is a valid target for a skill, including lane restrictions for melee skills.
        /// </summary>
        public static bool IsValidTarget(Skill skill, CharacterInstance character, CharacterInstance caster = null)
        {
            if (skill == null || character == null) return false;

            // Check basic target type validation first
            bool isEnemy = character.gameObject.CompareTag("Enemy");
            bool isPlayer = character.gameObject.CompareTag("Player");

            Debug.Log($"TargetingUtility: IsValidTarget called with skill: {skill.skillName}, character: {character.GetCharacterName()}, caster: {caster?.GetCharacterName()}");
            Debug.Log($"TargetingUtility: isEnemy: {isEnemy}, isPlayer: {isPlayer}");
            Debug.Log($"TargetingUtility: skill.targetType: {skill.targetType}");

            bool basicValid = false;
            switch (skill.targetType)
            {
                case SkillTargetType.Enemy:
                    if (caster != null)
                    {
                        bool casterIsEnemy = caster.gameObject.CompareTag("Enemy");
                        basicValid = casterIsEnemy ? isPlayer : isEnemy;
                    }
                    else
                    {
                        basicValid = isEnemy;
                    }
                    break;
                case SkillTargetType.Ally:
                    if (caster != null)
                    {
                        bool casterIsEnemy = caster.gameObject.CompareTag("Enemy");
                        basicValid = casterIsEnemy ? isEnemy : isPlayer;
                    }
                    else
                    {
                        basicValid = isPlayer;
                    }
                    break;
                case SkillTargetType.Self:
                    basicValid = caster != null ? character == caster : true;
                    break;
                case SkillTargetType.AllEnemies:
                    basicValid = isEnemy;
                    break;
                case SkillTargetType.AllAllies:
                    basicValid = isPlayer;
                    break;
                default:
                    return false;
            }

            if (!basicValid) return false;

            // Check skill's allowed lane restriction (if set)
            if (skill.allowedLane > 0)
            {
                int targetLane = GetLaneNumber(character);

                // Debug logging
                if (caster != null)
                {
                    string parentName = character.transform.parent != null ? character.transform.parent.name : "NO PARENT";
                    Debug.Log($"TargetingUtility: Skill '{skill.skillName}' (allowedLane={skill.allowedLane}) - Target '{character.GetCharacterName()}' parent='{parentName}', detected lane={targetLane}");
                }

                if (targetLane != skill.allowedLane)
                {
                    if (targetLane == 0)
                    {
                        Debug.LogWarning($"TargetingUtility: Could not determine lane for {character.GetCharacterName()} - parent name might not contain 'Lane1' or 'Lane2'");
                    }
                    return false; // Target is not in the allowed lane
                }
            }

            // Check lane restrictions for melee skills
            if (skill.skillType == SkillType.Melee && caster != null)
            {
                // Determine which tag we're looking for (enemy or player)
                string targetTag = caster.gameObject.CompareTag("Enemy") ? "Player" : "Enemy";

                // Check if Lane1 is empty
                bool lane1Empty = IsLaneEmpty(1, targetTag);

                // Get target's lane
                int targetLane = GetLaneNumber(character);

                // Melee can only target Lane1, unless Lane1 is empty, then can target Lane2
                if (lane1Empty)
                {
                    // Lane1 is empty, can target Lane2
                    return targetLane == 2;
                }
                else
                {
                    // Lane1 has characters, can only target Lane1
                    return targetLane == 1;
                }
            }

            return true; // Non-melee skills or no lane restrictions
        }
    }
}
