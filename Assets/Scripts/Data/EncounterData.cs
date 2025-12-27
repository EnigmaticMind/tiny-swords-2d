using System.Collections.Generic;
using UnityEngine;

namespace TinySwords2D.Data
{
    [System.Serializable]
    public class EnemySpawnRequest
    {
        [Tooltip("The enemy prefab to spawn (e.g., SkeletonContainer, TrollContainer)")]
        public GameObject enemyPrefab;
        
        [Tooltip("The character definition for this enemy instance")]
        public CharacterDefinition characterDefinition;
        
        [Tooltip("How many of this enemy to spawn")]
        public int count = 1;
        
        [Tooltip("Which lane to spawn in (1 = Melee lane, 2 = Ranged lane)")]
        public int laneNumber = 1;
    }

    [CreateAssetMenu(menuName = "Tiny Swords/Encounter Data", fileName = "NewEncounter")]
    public class EncounterData : ScriptableObject
    {
        [Header("Encounter Info")]
        [Tooltip("Name/description of this encounter")]
        public string encounterName = "New Encounter";

        [Header("Appearance Window")]
        [Tooltip("First encounter number where this encounter can appear (1-based, so 1 = first encounter)")]
        public int firstEncounter = 1;
        
        [Tooltip("Last encounter number where this encounter can appear (0 = appears forever)")]
        public int lastEncounter = 0;

        [Header("Enemy Spawns")]
        [Tooltip("List of enemy spawn requests (prefab, definition, count, lane)")]
        public List<EnemySpawnRequest> enemySpawns = new List<EnemySpawnRequest>();

        /// <summary>
        /// Checks if this encounter is available at the given encounter index (0-based).
        /// </summary>
        public bool IsAvailableAtEncounter(int encounterIndex)
        {
            int encounterNumber = encounterIndex + 1; // Convert to 1-based
            
            if (encounterNumber < firstEncounter)
            {
                return false; // Too early
            }
            
            if (lastEncounter > 0 && encounterNumber > lastEncounter)
            {
                return false; // Too late
            }
            
            return true;
        }
    }
}
