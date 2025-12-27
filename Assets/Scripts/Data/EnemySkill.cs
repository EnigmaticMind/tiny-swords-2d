using UnityEngine;

namespace TinySwords2D.Data
{
    /// <summary>
    /// Skill for enemy characters. Inherits from Skill and adds organized menu structure.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiny Swords/Skills/Enemy Skill", fileName = "NewEnemySkill", order = 1)]
    public class EnemySkill : Skill
    {
        // Inherits all functionality from Skill
        // This class exists primarily to organize the Unity menu
    }
}

