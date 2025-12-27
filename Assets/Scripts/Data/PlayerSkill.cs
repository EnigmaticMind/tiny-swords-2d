using UnityEngine;

namespace TinySwords2D.Data
{
    /// <summary>
    /// Skill for player characters. Inherits from Skill and adds organized menu structure.
    /// </summary>
    [CreateAssetMenu(menuName = "Tiny Swords/Skills/Player Skill", fileName = "NewPlayerSkill", order = 2)]
    public class PlayerSkill : Skill
    {
        // Inherits all functionality from Skill
        // This class exists primarily to organize the Unity menu
    }
}

