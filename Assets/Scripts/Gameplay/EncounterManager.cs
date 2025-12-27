using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TinySwords2D.Data;
using TinySwords2D.UI; // Add this line

namespace TinySwords2D.Gameplay
{
    /// <summary>
    /// Manages encounters - spawning enemies and transitioning between encounters.
    /// </summary>
    public class EncounterManager : MonoBehaviour
    {
        public static EncounterManager Instance { get; private set; }

        [Header("Encounter Settings")]
        [Tooltip("List of all possible encounters. The system will randomly select from available encounters based on appearance windows.")]
        [SerializeField] private List<EncounterData> allEncounters = new List<EncounterData>();

        [Header("Lane References")]
        [Tooltip("Parent GameObject for Lane 1 (Melee lane). Enemies will be spawned as children.")]
        [SerializeField] private Transform lane1Parent;

        [Tooltip("Parent GameObject for Lane 2 (Ranged lane). Enemies will be spawned as children.")]
        [SerializeField] private Transform lane2Parent;

        // Events
        [Tooltip("Called when all enemies in current encounter are defeated")]
        public event Action OnEncounterComplete;

        [Tooltip("Called when transitioning to next encounter (before spawning enemies). Use this for menus.")]
        public event Action<EncounterData, int> OnEncounterStarting; // Passes encounter data and encounter number

        [Tooltip("Called when all encounters are complete")]
        public event Action OnAllEncountersComplete;

        private int currentEncounterIndex = 0;
        private List<CharacterInstance> currentEnemyInstances = new List<CharacterInstance>();
        private bool isHandlingCompletion = false; // Add this flag

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

        private void Start()
        {
            // Auto-find lane parents if not assigned
            if (lane1Parent == null)
            {
                GameObject lane1 = GameObject.Find("Lane1") ?? GameObject.Find("Lane 1");
                if (lane1 != null) lane1Parent = lane1.transform;
            }

            if (lane2Parent == null)
            {
                GameObject lane2 = GameObject.Find("Lane2") ?? GameObject.Find("Lane 2");
                if (lane2 != null) lane2Parent = lane2.transform;
            }

            // Start first encounter
            if (allEncounters.Count > 0)
            {
                StartCoroutine(StartEncounter(0));
            }
            else
            {
                Debug.LogWarning("EncounterManager: No encounters assigned!");
            }
        }

        private void Update()
        {
            // Check if all enemies are dead (and we're not already handling completion)
            if (!isHandlingCompletion && currentEnemyInstances.Count > 0 && AreAllEnemiesDead())
            {
                isHandlingCompletion = true; // Set flag to prevent multiple calls
                OnEncounterComplete?.Invoke();
                StartCoroutine(HandleEncounterComplete());
            }
        }

        /// <summary>
        /// Starts a specific encounter by index.
        /// </summary>
        private IEnumerator StartEncounter(int encounterIndex)
        {
            // Get available encounters for this encounter number
            List<EncounterData> availableEncounters = GetAvailableEncounters(encounterIndex);

            if (availableEncounters.Count == 0)
            {
                Debug.LogWarning($"EncounterManager: No available encounters at encounter {encounterIndex + 1}");
                OnAllEncountersComplete?.Invoke();
                yield break;
            }

            // Select a random encounter from available ones
            EncounterData encounter = availableEncounters[UnityEngine.Random.Range(0, availableEncounters.Count)];
            currentEncounterIndex = encounterIndex;

            int encounterNumber = encounterIndex + 1;
            Debug.Log($"EncounterManager: Starting encounter {encounterNumber}: {encounter.encounterName}");

            // Fire event for menus (before spawning)
            OnEncounterStarting?.Invoke(encounter, encounterNumber);

            // Wait a frame to allow menus to show
            yield return null;

            // Clear previous enemies
            ClearCurrentEnemies();

            // Spawn enemies
            SpawnEncounterEnemies(encounter);

            // Update lane positions after spawning
            UpdateLanePositions();

            // Notify TurnManager to refresh enemy list and start player turn
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RefreshEnemyList();

                // Start player turn after enemies are spawned (enemies will plan their moves)
                TurnManager.Instance.StartPlayerTurn();
            }
        }

        /// <summary>
        /// Gets all encounters that are available at the given encounter index.
        /// </summary>
        private List<EncounterData> GetAvailableEncounters(int encounterIndex)
        {
            List<EncounterData> available = new List<EncounterData>();

            foreach (EncounterData encounter in allEncounters)
            {
                if (encounter != null && encounter.IsAvailableAtEncounter(encounterIndex))
                {
                    available.Add(encounter);
                }
            }

            return available;
        }

        /// <summary>
        /// Spawns all enemies for the current encounter.
        /// </summary>
        private void SpawnEncounterEnemies(EncounterData encounter)
        {
            currentEnemyInstances.Clear();

            foreach (EnemySpawnRequest spawnRequest in encounter.enemySpawns)
            {
                if (spawnRequest.enemyPrefab == null)
                {
                    Debug.LogWarning($"EncounterManager: Enemy prefab is null in encounter {encounter.encounterName}");
                    continue;
                }

                // Determine which lane parent to use
                Transform laneParent = GetLaneParent(spawnRequest.laneNumber);
                if (laneParent == null)
                {
                    Debug.LogError($"EncounterManager: Could not find parent for Lane {spawnRequest.laneNumber}");
                    continue;
                }

                // Spawn multiple instances if count > 1
                for (int i = 0; i < spawnRequest.count; i++)
                {
                    GameObject enemyObj = Instantiate(spawnRequest.enemyPrefab, laneParent);
                    enemyObj.name = $"{spawnRequest.enemyPrefab.name}_{i + 1}";

                    // Set character definition if provided
                    CharacterInstance characterInstance = enemyObj.GetComponentInChildren<CharacterInstance>();
                    if (characterInstance != null)
                    {
                        if (spawnRequest.characterDefinition != null)
                        {
                            characterInstance.Definition = spawnRequest.characterDefinition;
                        }
                        currentEnemyInstances.Add(characterInstance);
                    }
                    else
                    {
                        Debug.LogWarning($"EncounterManager: Spawned enemy {spawnRequest.enemyPrefab.name} has no CharacterInstance component!");
                    }
                }
            }

            Debug.Log($"EncounterManager: Spawned {currentEnemyInstances.Count} enemies for encounter {encounter.encounterName}");

            // Update lane positions after spawning
            UpdateLanePositions();
        }

        /// <summary>
        /// Updates positions for all lanes using LaneAutoPositioner.
        /// </summary>
        private void UpdateLanePositions()
        {
            if (lane1Parent != null)
            {
                LaneAutoPositioner positioner1 = lane1Parent.GetComponent<LaneAutoPositioner>();
                if (positioner1 != null)
                {
                    positioner1.RepositionChildren();
                }
            }

            if (lane2Parent != null)
            {
                LaneAutoPositioner positioner2 = lane2Parent.GetComponent<LaneAutoPositioner>();
                if (positioner2 != null)
                {
                    positioner2.RepositionChildren();
                }
            }
        }

        /// <summary>
        /// Gets the parent transform for a given lane number.
        /// </summary>
        private Transform GetLaneParent(int laneNumber)
        {
            switch (laneNumber)
            {
                case 1:
                    return lane1Parent;
                case 2:
                    return lane2Parent;
                default:
                    Debug.LogWarning($"EncounterManager: Invalid lane number {laneNumber}, defaulting to Lane 1");
                    return lane1Parent;
            }
        }

        /// <summary>
        /// Checks if all enemies in the current encounter are dead.
        /// </summary>
        private bool AreAllEnemiesDead()
        {
            foreach (CharacterInstance enemy in currentEnemyInstances)
            {
                if (enemy != null && !enemy.isDead)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Handles encounter completion - shows skill selection, then transitions to next encounter.
        /// </summary>
        private IEnumerator HandleEncounterComplete()
        {
            Debug.Log($"EncounterManager: Encounter {currentEncounterIndex + 1} complete!");

            // Wait a moment for death animations
            yield return new WaitForSeconds(1f);

            // Reset player character stats at end of battle
            ResetPlayerStats();

            // Check if there are more available encounters
            List<EncounterData> nextAvailable = GetAvailableEncounters(currentEncounterIndex + 1);
            if (nextAvailable.Count > 0)
            {
                // Show skill selection before next encounter
                if (MenuManager.Instance != null)
                {
                    bool skillSelectionCompleted = false;

                    // Subscribe to skill selection completion
                    System.Action onComplete = () => { skillSelectionCompleted = true; };
                    MenuManager.Instance.OnSkillSelectionComplete += onComplete;

                    // Open skill selection scene (pauses game)
                    MenuManager.Instance.OpenSkillSelection();

                    // Wait for skill selection to complete (resume button clicked)
                    yield return new WaitUntil(() => skillSelectionCompleted);

                    // Unsubscribe
                    MenuManager.Instance.OnSkillSelectionComplete -= onComplete;
                }

                // Start next encounter (will randomly select from available)
                yield return StartCoroutine(StartEncounter(currentEncounterIndex + 1));
            }
            else
            {
                // No more encounters available
                Debug.Log("EncounterManager: All encounters complete!");
                OnAllEncountersComplete?.Invoke();
            }

            // Reset flag after handling is complete
            isHandlingCompletion = false;
        }

        /// <summary>
        /// Resets player character stats at the end of a battle.
        /// Currently resets armor to 0.
        /// </summary>
        private void ResetPlayerStats()
        {
            // Find all player characters in the scene
            CharacterInstance[] allCharacters = FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);

            foreach (CharacterInstance character in allCharacters)
            {
                if (character != null && character.gameObject.CompareTag("Player"))
                {
                    // Reset armor to 0
                    character.currentArmor = 0;
                    Debug.Log($"EncounterManager: Reset armor for {character.GetCharacterName()} to 0");
                }
            }

            Debug.Log("EncounterManager: Reset all player character stats");
        }

        /// <summary>
        /// Clears all current enemy instances from the scene.
        /// </summary>
        private void ClearCurrentEnemies()
        {
            // Destroy all tracked enemies
            foreach (CharacterInstance enemy in currentEnemyInstances)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            currentEnemyInstances.Clear();

            // Also clear all enemies from lane parents (in case any were missed)
            if (lane1Parent != null)
            {
                ClearLaneChildren(lane1Parent);
            }

            if (lane2Parent != null)
            {
                ClearLaneChildren(lane2Parent);
            }
        }

        /// <summary>
        /// Clears all enemy children from a lane parent.
        /// </summary>
        private void ClearLaneChildren(Transform laneParent)
        {
            // Destroy all children of the lane (we control what spawns here, so all children are enemies)
            for (int i = laneParent.childCount - 1; i >= 0; i--)
            {
                Transform child = laneParent.GetChild(i);
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Public method to manually start a specific encounter (useful for testing).
        /// </summary>
        public void StartEncounterManually(int encounterIndex)
        {
            StartCoroutine(StartEncounter(encounterIndex));
        }

        /// <summary>
        /// Gets the current encounter number (1-based).
        /// </summary>
        public int GetCurrentEncounterNumber()
        {
            return currentEncounterIndex + 1;
        }
    }
}
