using UnityEngine;

namespace TinySwords2D.Data


{
    public enum SkillTargetType
    {
        Self,
        Ally,
        Enemy,
        AllAllies,
        AllEnemies
    }

    public enum SkillType
    {
        Buff,
        Melee,
        Ranged
    }
    [CreateAssetMenu(menuName = "Tiny Swords/Skills/Skill", fileName = "NewSkill", order = 0)]
    public class Skill : ScriptableObject
    {
        [Header("Identification")]
        public string skillName = "New Skill";
        [TextArea] public string description;
        public Sprite icon;

        [Header("Tooltip")]
        [TextArea(3, 6)]
        [Tooltip("Tooltip text with placeholders. Use {targetDamage}, {targetArmor}, {selfDamage}, {selfArmor}, {staminaRequirement}, {targetType}, {damageReduction}, {cancelsAction}")]
        public string tooltipText = "Deals {targetDamage} damage";

        [Header("Gameplay Stats")]
        public SkillType skillType = SkillType.Melee;
        // damage it deals to target, can be negative for healing
        public int targetDamage;
        // armor it applied to target, can be negative for debuff
        public int targetArmor;
        // healing it does to self, can be negative for damage
        public int selfDamage;
        // armor it applied to self, can be negative for debuff
        public int selfArmor;

        // stamina required to use this skill
        [Tooltip("Stamina required to use this skill. Skills start at 100% stamina.")]
        public int staminaRequirement = 100;

        [Tooltip("If true, this skill's damage ignores armor and goes directly to health.")]
        public bool ignoresArmor = false;

        [Tooltip("If true, this skill's damage only affects armor and never reduces health.")]
        public bool armorOnly = false;

        [Tooltip("Flat damage reduction applied to target's damage output. Reduces damage by this amount (e.g., 4 = reduces damage by 4).")]
        public int damageReduction = 0;

        [Tooltip("If true, this skill cancels the target's planned action for this turn (only affects enemies).")]
        public bool cancelsAction = false;

        [Tooltip("If true, this skill forces the target to attack only the caster (taunt/intercept effect, only affects enemies).")]
        public bool interceptsAttack = false;

        public SkillTargetType targetType = SkillTargetType.Enemy;

        [Header("Target Restrictions")]
        [Tooltip("Allowed lane for targeting. 0 = Any lane, 1 = Lane1 only, 2 = Lane2 only. Empty/0 means skill can target either lane.")]
        [Range(0, 2)]
        public int allowedLane = 0;

        [Header("AI Weight (Enemy Only)")]
        [Tooltip("Weight for enemy AI skill selection. Higher weight = more likely to be chosen. Default is 1.0 (equal chance).")]
        [Range(0f, 100f)]
        public float weight = 1f;

        [Header("Animation")]
        [Tooltip("Default animation trigger name. Can be overridden per character.")]
        public string animationTrigger = "Attack";

    }
}

