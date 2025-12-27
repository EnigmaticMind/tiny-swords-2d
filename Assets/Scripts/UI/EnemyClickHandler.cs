using UnityEngine;
using UnityEngine.EventSystems;
using TinySwords2D.Gameplay;

/// <summary>
/// Handles clicking on enemies for targeting.
/// Add this to enemy GameObjects.
/// </summary>
[RequireComponent(typeof(CharacterInstance))]
public class EnemyClickHandler : MonoBehaviour, IPointerClickHandler
{
    private CharacterInstance characterInstance;

    private void Awake()
    {
        characterInstance = GetComponent<CharacterInstance>();

        if (characterInstance == null)
        {
            Debug.LogError($"EnemyClickHandler: No CharacterInstance found on {gameObject.name}!");
        }

        // Ensure enemy is tagged/layered correctly
        if (!gameObject.CompareTag("Enemy"))
        {
            gameObject.tag = "Enemy";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if it's player turn
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentTurn != TurnManager.TurnState.PlayerTurn)
        {
            return;
        }

        if (characterInstance == null) return;

        // Check if we're in targeting mode
        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting())
        {
            TargetingManager.Instance.SelectTarget(characterInstance);
        }
    }

    // Alternative for non-UI enemies (world-space sprites)
    private void OnMouseDown()
    {
        if (characterInstance == null) return;

        if (TargetingManager.Instance != null && TargetingManager.Instance.IsTargeting())
        {
            TargetingManager.Instance.SelectTarget(characterInstance);
        }
    }
}
