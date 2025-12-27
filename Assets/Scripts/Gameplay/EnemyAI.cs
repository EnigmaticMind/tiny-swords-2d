using UnityEngine;
using TinySwords2D.Data;
using System.Collections.Generic;

namespace TinySwords2D.Gameplay
{
  /// <summary>
  /// AI component for enemy characters. Handles planning and executing moves.
  /// Only attached to enemy GameObjects.
  /// </summary>
  [RequireComponent(typeof(CharacterInstance))]
  public class EnemyAI : MonoBehaviour
  {
    [System.Serializable]
    public class PlannedMove
    {
      public Skill skill;
      public CharacterInstance target;

      public PlannedMove(Skill skill, CharacterInstance target)
      {
        this.skill = skill;
        this.target = target;
      }
    }

    private CharacterInstance characterInstance;
    private PlannedMove plannedMove;

    public CharacterInstance CharacterInstance => characterInstance;

    private void Awake()
    {
      characterInstance = GetComponent<CharacterInstance>();
      if (characterInstance == null)
      {
        Debug.LogError("EnemyAI: CharacterInstance component required!");
      }
    }

    /// <summary>
    /// Plans a move for this enemy. Updates attackPlan to display what they'll do.
    /// </summary>
    public void PlanMove()
    {
      if (characterInstance == null || characterInstance.Definition == null)
      {
        Debug.LogWarning("EnemyAI: Cannot plan move - CharacterInstance or Definition is null");
        return;
      }

      // Skip planning if enemy is dead
      if (characterInstance.isDead)
      {
        characterInstance.attackPlan = "";
        plannedMove = null;
        return;
      }

      // Get available skills
      List<Skill> availableSkills = characterInstance.GetSkills();
      if (availableSkills == null || availableSkills.Count == 0)
      {
        characterInstance.attackPlan = "No skills";
        return;
      }

      // Weighted random skill selection based on skill weights
      Skill chosenSkill = ChooseWeightedSkill(availableSkills);

      // Find a valid target
      CharacterInstance target = FindTarget(chosenSkill);

      // Store the planned move
      plannedMove = new PlannedMove(chosenSkill, target);

      // Update attack plan display (this will show in CharacterUI)
      if (target != null)
      {
        string effects = FormatSkillEffects(chosenSkill, target);
        characterInstance.attackPlan = $"{chosenSkill.skillName} → {target.GetCharacterName()}{effects}";
      }
      else if (chosenSkill.targetType == SkillTargetType.Self)
      {
        string effects = FormatSelfEffects(chosenSkill);
        characterInstance.attackPlan = $"{chosenSkill.skillName} (Self){effects}";
      }
      else if (chosenSkill.targetType == SkillTargetType.AllEnemies || chosenSkill.targetType == SkillTargetType.AllAllies)
      {
        string effects = FormatSkillEffects(chosenSkill, null);
        characterInstance.attackPlan = $"{chosenSkill.skillName} (All){effects}";
      }
      else
      {
        string effects = FormatSkillEffects(chosenSkill, null);
        characterInstance.attackPlan = $"{chosenSkill.skillName}{effects}";
      }

      Debug.Log($"{characterInstance.GetCharacterName()} plans: {characterInstance.attackPlan}");
    }

    /// <summary>
    /// Chooses a skill using weighted random selection based on skill weights.
    /// Skills with higher weights are more likely to be chosen.
    /// </summary>
    private Skill ChooseWeightedSkill(List<Skill> skills)
    {
      if (skills == null || skills.Count == 0)
      {
        return null;
      }

      // Calculate total weight
      float totalWeight = 0f;
      foreach (Skill skill in skills)
      {
        // Use weight, but ensure it's at least 0.1 to give all skills a chance
        totalWeight += Mathf.Max(skill.weight, 0.1f);
      }

      // If total weight is 0 or very small, fall back to uniform random
      if (totalWeight <= 0.01f)
      {
        return skills[Random.Range(0, skills.Count)];
      }

      // Pick a random value between 0 and totalWeight
      float randomValue = Random.Range(0f, totalWeight);

      // Find which skill this random value corresponds to
      float currentWeight = 0f;
      foreach (Skill skill in skills)
      {
        currentWeight += Mathf.Max(skill.weight, 0.1f);
        if (randomValue <= currentWeight)
        {
          return skill;
        }
      }

      // Fallback (shouldn't reach here, but just in case)
      return skills[skills.Count - 1];
    }

    private CharacterInstance FindTarget(Skill skill)
    {
      // Find all player characters
      CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
      List<CharacterInstance> validTargets = new List<CharacterInstance>();

      foreach (CharacterInstance character in allCharacters)
      {
        if (character.gameObject.CompareTag("Player"))
        {
          if (TargetingUtility.IsValidTarget(skill, character, characterInstance))
          {
            validTargets.Add(character);
          }
        }
      }

      // Pick random valid target (you can implement smarter targeting later)
      if (validTargets.Count > 0)
      {
        return validTargets[Random.Range(0, validTargets.Count)];
      }

      return null;
    }

    public bool HasPlannedMove()
    {
      return plannedMove != null && plannedMove.skill != null;
    }

    public PlannedMove GetPlannedMove()
    {
      return plannedMove;
    }

    public void ClearPlannedMove()
    {
      plannedMove = null;
      if (characterInstance != null)
      {
        characterInstance.attackPlan = "";
      }
    }

    /// <summary>
    /// Changes the target of the planned move to a new target and updates the attack plan display.
    /// </summary>
    public void ChangePlannedMoveTarget(CharacterInstance newTarget)
    {
      if (plannedMove == null || plannedMove.skill == null)
      {
        Debug.LogWarning($"EnemyAI: Cannot change target - no planned move exists for {characterInstance.GetCharacterName()}");
        return;
      }

      // Update the planned move target
      plannedMove.target = newTarget;

      // Update attack plan display
      if (newTarget != null)
      {
        string effects = FormatSkillEffects(plannedMove.skill, newTarget);
        characterInstance.attackPlan = $"{plannedMove.skill.skillName} → {newTarget.GetCharacterName()}{effects}";
        Debug.Log($"{characterInstance.GetCharacterName()}'s attack redirected to {newTarget.GetCharacterName()}");
      }
      else
      {
        characterInstance.attackPlan = $"{plannedMove.skill.skillName}";
      }
    }

    /// <summary>
    /// Formats skill effects for display in attack plan
    /// </summary>
    private string FormatSkillEffects(Skill skill, CharacterInstance target)
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      bool hasEffects = false;

      // Target damage/healing
      if (skill.targetDamage != 0)
      {
        if (skill.targetDamage > 0)
        {
          // Apply damage reduction from the caster (this enemy) to show actual damage
          int actualDamage = Mathf.Max(0, skill.targetDamage - characterInstance.damageReduction);
          sb.Append($" ({actualDamage} dmg)");
        }
        else
        {
          sb.Append($" (+{Mathf.Abs(skill.targetDamage)} heal)");
        }
        hasEffects = true;
      }

      // Target armor changes
      if (skill.targetArmor != 0)
      {
        if (skill.targetArmor > 0)
        {
          sb.Append($" (+{skill.targetArmor} armor)");
        }
        else
        {
          sb.Append($" ({skill.targetArmor} armor)");
        }
        hasEffects = true;
      }

      // Self effects (if targeting others)
      if (target != null && (skill.selfDamage != 0 || skill.selfArmor != 0))
      {
        if (skill.selfDamage != 0)
        {
          if (skill.selfDamage > 0)
          {
            sb.Append($" (self: {skill.selfDamage} dmg)");
          }
          else
          {
            sb.Append($" (self: +{Mathf.Abs(skill.selfDamage)} heal)");
          }
        }
        if (skill.selfArmor != 0)
        {
          if (skill.selfArmor > 0)
          {
            sb.Append($" (self: +{skill.selfArmor} armor)");
          }
          else
          {
            sb.Append($" (self: {skill.selfArmor} armor)");
          }
        }
      }

      return sb.ToString();
    }

    /// <summary>
    /// Formats self-only effects for display
    /// </summary>
    private string FormatSelfEffects(Skill skill)
    {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      bool hasEffects = false;

      if (skill.selfDamage != 0)
      {
        if (skill.selfDamage > 0)
        {
          sb.Append($" ({skill.selfDamage} self dmg)");
        }
        else
        {
          sb.Append($" (+{Mathf.Abs(skill.selfDamage)} self heal)");
        }
        hasEffects = true;
      }

      if (skill.selfArmor != 0)
      {
        if (skill.selfArmor > 0)
        {
          sb.Append($" (+{skill.selfArmor} self armor)");
        }
        else
        {
          sb.Append($" ({skill.selfArmor} self armor)");
        }
        hasEffects = true;
      }

      return sb.ToString();
    }

    /// <summary>
    /// Updates the attack plan display to reflect current stats (e.g., damage reduction)
    /// </summary>
    public void UpdateAttackPlanDisplay()
    {
      if (plannedMove == null || plannedMove.skill == null || characterInstance == null)
      {
        return;
      }

      Skill skill = plannedMove.skill;
      CharacterInstance target = plannedMove.target;

      // Update attack plan display with current damage values
      if (target != null)
      {
        string effects = FormatSkillEffects(skill, target);
        characterInstance.attackPlan = $"{skill.skillName} → {target.GetCharacterName()}{effects}";
      }
      else if (skill.targetType == SkillTargetType.Self)
      {
        string effects = FormatSelfEffects(skill);
        characterInstance.attackPlan = $"{skill.skillName} (Self){effects}";
      }
      else if (skill.targetType == SkillTargetType.AllEnemies || skill.targetType == SkillTargetType.AllAllies)
      {
        string effects = FormatSkillEffects(skill, null);
        characterInstance.attackPlan = $"{skill.skillName} (All){effects}";
      }
      else
      {
        string effects = FormatSkillEffects(skill, null);
        characterInstance.attackPlan = $"{skill.skillName}{effects}";
      }
    }
  }
}
