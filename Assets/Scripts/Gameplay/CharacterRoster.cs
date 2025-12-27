using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TinySwords2D.Data;

namespace TinySwords2D.Gameplay
{
    public class CharacterRoster : MonoBehaviour
    {
        public static CharacterRoster Instance { get; private set; }

        [Header("Available Characters")]
        [SerializeField] private List<CharacterDefinition> availableCharacters = new();
        [SerializeField] private CharacterDefinition defaultCharacter;

        [Header("Targeting")]
        [SerializeField] private TargetingManager targetingManager;

        // Source of truth for active character
        public CharacterDefinition ActiveCharacter { get; private set; }
        public CharacterInstance ActiveCharacterInstance { get; private set; }

        [System.Serializable]
        public class CharacterChangedEvent : UnityEvent<CharacterDefinition> { }

        public CharacterChangedEvent OnActiveCharacterChanged = new();

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Debug.Log("CharacterRoster: Awake called");
            Debug.Log($"CharacterRoster: DefaultCharacter is {(defaultCharacter != null ? defaultCharacter.characterName : "NULL")}");
            Debug.Log($"CharacterRoster: AvailableCharacters count = {availableCharacters.Count}");

            if (defaultCharacter == null && availableCharacters.Count > 0)
            {
                // Select a random character instead of always using the first one
                defaultCharacter = availableCharacters[Random.Range(0, availableCharacters.Count)];
                Debug.Log($"CharacterRoster: Selected random character: {defaultCharacter.characterName}");
            }
        }

        public void SetActiveCharacter(CharacterInstance specificInstance)
        {

            if (specificInstance == null || specificInstance.Definition == null)
            {
                Debug.LogWarning("CharacterRoster: Cannot set active character - instance or definition is null");
                return;
            }

            // Cancel targeting if active
            if (targetingManager != null && targetingManager.IsTargeting())
            {
                targetingManager.CancelTargeting();
            }

            Debug.Log($"CharacterRoster: SetActiveCharacter called with {(specificInstance != null ? specificInstance.GetCharacterName() : "NULL")}");

            if (ActiveCharacter == specificInstance.Definition && ActiveCharacterInstance == specificInstance)
            {
                Debug.Log("CharacterRoster: Character is already active, skipping");
                return;
            }

            ActiveCharacter = specificInstance.Definition;
            ActiveCharacterInstance = specificInstance;

            if (!availableCharacters.Contains(specificInstance.Definition))
            {
                availableCharacters.Add(specificInstance.Definition);
            }

            Debug.Log($"CharacterRoster: Using provided instance: {specificInstance.GetCharacterName()}");
            Debug.Log($"CharacterRoster: Invoking OnActiveCharacterChanged event");
            OnActiveCharacterChanged?.Invoke(ActiveCharacter);
            Debug.Log("CharacterRoster: Event invoked");
        }

        /// <summary>
        /// Switches to the next available player character that hasn't acted this turn.
        /// If all characters have acted, does nothing.
        /// </summary>
        public void NextActiveCharacter()
        {
            // Find all player characters in the scene
            CharacterInstance[] allCharacters = Object.FindObjectsByType<CharacterInstance>(FindObjectsSortMode.None);
            List<CharacterInstance> playerCharacters = new List<CharacterInstance>();

            foreach (CharacterInstance character in allCharacters)
            {
                if (character != null && character.gameObject.CompareTag("Player"))
                {
                    playerCharacters.Add(character);
                }
            }

            if (playerCharacters.Count == 0)
            {
                Debug.LogWarning("CharacterRoster: No player characters found in scene");
                return;
            }

            // Find current active character index
            int currentIndex = -1;
            if (ActiveCharacterInstance != null)
            {
                currentIndex = playerCharacters.IndexOf(ActiveCharacterInstance);
            }

            // Start searching from the next character (or from the beginning if currentIndex is -1)
            int startIndex = currentIndex + 1;
            if (startIndex >= playerCharacters.Count)
            {
                startIndex = 0;
            }

            // Search for next character that hasn't acted
            for (int i = 0; i < playerCharacters.Count; i++)
            {
                int index = (startIndex + i) % playerCharacters.Count;
                CharacterInstance nextCharacter = playerCharacters[index];

                // Skip if this is the current active character
                if (nextCharacter == ActiveCharacterInstance)
                {
                    continue;
                }

                // If character hasn't acted, switch to them
                if (!nextCharacter.hasActedThisTurn)
                {
                    Debug.Log($"CharacterRoster: Switching to next available character: {nextCharacter.GetCharacterName()}");
                    SetActiveCharacter(nextCharacter);
                    return;
                }
            }

            // No available characters found
            Debug.Log("CharacterRoster: No available characters found - all have acted this turn");
        }
    }
}

