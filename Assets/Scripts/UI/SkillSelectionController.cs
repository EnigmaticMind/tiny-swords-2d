using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace TinySwords2D.UI
{
  /// <summary>
  /// Controls the skill selection UI. Lives in the SkillSelection scene.
  /// </summary>
  public class SkillSelectionController : MonoBehaviour
  {
    [Header("UI References")]
    [SerializeField] private Button resumeButton;

    private MenuManager menuManager;

    /// <summary>
    /// Called by MenuManager when the skill selection scene is loaded
    /// </summary>
    public void Initialize(MenuManager manager)
    {
      menuManager = manager;

      // Setup resume button
      if (resumeButton != null)
      {
        resumeButton.onClick.RemoveAllListeners();
        resumeButton.onClick.AddListener(ResumeGame);
      }
      else
      {
        Debug.LogWarning("SkillSelectionController: Resume button not assigned!");
      }
    }

    private void Update()
    {
      // ESC key can also resume (optional)
      if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        ResumeGame();
      }
    }

    /// <summary>
    /// Resumes the game and continues to next encounter
    /// </summary>
    public void ResumeGame()
    {
      if (menuManager != null)
      {
        menuManager.OnSkillSelectionResume();
      }
    }
  }
}
