using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TinySwords2D.Gameplay; // Add this for EncounterManager and MenuManager
using UnityEngine.EventSystems;
using System.Collections; // Add this for IEnumerator

namespace TinySwords2D.UI
{
  /// <summary>
  /// Controls the pause menu UI. Lives in the PauseMenuScene.
  /// </summary>
  public class PauseMenuController : MonoBehaviour
  {
    [Header("Menu References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button restartButton;

    [Header("Backdrop")]
    [SerializeField] private GameObject backdropObject;

    [Header("Confirmation Modal")]
    [SerializeField] private ConfirmationModal confirmationModal;

    private MenuManager menuManager;

    /// <summary>
    /// Called by MenuManager when the menu scene is loaded
    /// </summary>
    public void Initialize(MenuManager manager)
    {
      menuManager = manager;

      // Auto-find confirmation modal if not assigned
      if (confirmationModal == null)
      {
        confirmationModal = FindFirstObjectByType<ConfirmationModal>();
      }

      // Auto-find backdrop if not assigned
      if (backdropObject == null)
      {
        // Try to find a backdrop by name
        Transform backdropObj = transform.root.Find("Backdrop") ?? transform.root.Find("Background");
        if (backdropObj != null)
        {
          backdropObject = backdropObj.gameObject;
        }
      }

      // Setup backdrop click handler using IPointerClickHandler
      if (backdropObject != null)
      {
        BackdropClickHandler handler = backdropObject.GetComponent<BackdropClickHandler>();
        if (handler == null)
        {
          handler = backdropObject.AddComponent<BackdropClickHandler>();
        }
        handler.Initialize(this);
      }

      // Setup button listeners
      if (resumeButton != null)
      {
        resumeButton.onClick.RemoveAllListeners();
        resumeButton.onClick.AddListener(ResumeGame);
      }

      if (restartButton != null)
      {
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(ShowRestartConfirmation);
      }

      if (exitButton != null)
      {
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(ShowExitConfirmation);
      }

      // Fix EventSystem first click issue
      StartCoroutine(FixEventSystemFirstClick());
    }

    private IEnumerator FixEventSystemFirstClick()
    {
      // Wait a frame for EventSystem to initialize
      yield return null;

      EventSystem eventSystem = EventSystem.current;
      if (eventSystem != null)
      {
        // Clear and reset selection
        eventSystem.SetSelectedGameObject(null);

        // Wait another frame
        yield return null;

        // Select resume button to ensure EventSystem is ready
        if (resumeButton != null)
        {
          eventSystem.SetSelectedGameObject(resumeButton.gameObject);
        }
      }
    }

    private void Update()
    {
      // ESC key handling - close modal first, then menu
      if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        if (confirmationModal != null && confirmationModal.IsOpen())
        {
          // Close the modal (calls OnNoClicked which hides it)
          confirmationModal.Hide();
        }
        else if (menuManager != null)
        {
          // Close the menu and resume game
          ResumeGame();
        }
      }
    }

    /// <summary>
    /// Resumes the game by closing the pause menu
    /// </summary>
    public void ResumeGame()
    {
      if (menuManager != null)
      {
        menuManager.CloseMenu();
      }
    }

    /// <summary>
    /// Called when backdrop is clicked - closes menu if modal is not open
    /// </summary>
    public void OnBackdropClicked()
    {
      // Only close menu if confirmation modal is not open
      if (confirmationModal == null || !confirmationModal.IsOpen())
      {
        ResumeGame();
      }
    }

    /// <summary>
    /// Shows confirmation modal for restart
    /// </summary>
    private void ShowRestartConfirmation()
    {
      if (confirmationModal != null)
      {
        confirmationModal.Show(
          "Are you sure you want to restart?",
          onConfirm: RestartGame,
          onCancel: null // Just closes modal, no action needed
        );
      }
      else
      {
        // Fallback: restart immediately if no modal
        Debug.LogWarning("PauseMenuController: No confirmation modal found, restarting immediately");
        RestartGame();
      }
    }

    /// <summary>
    /// Shows confirmation modal for exit
    /// </summary>
    private void ShowExitConfirmation()
    {
      if (confirmationModal != null)
      {
        confirmationModal.Show(
          "Are you sure you want to exit?",
          onConfirm: ExitGame,
          onCancel: null // Just closes modal, no action needed
        );
      }
      else
      {
        // Fallback: exit immediately if no modal
        Debug.LogWarning("PauseMenuController: No confirmation modal found, exiting immediately");
        ExitGame();
      }
    }

    /// <summary>
    /// Restarts the game (called after confirmation)
    /// </summary>
    private void RestartGame()
    {
      Time.timeScale = 1f; // Always unpause before scene operations

      // Find the battle scene (the scene that contains EncounterManager or MenuManager)
      // This is the scene we want to reload, not the pause menu scene
      Scene battleScene = default;

      // Check all loaded scenes to find the battle scene
      for (int i = 0; i < SceneManager.sceneCount; i++)
      {
        Scene scene = SceneManager.GetSceneAt(i);

        // Skip the pause menu and skill selection scenes
        if (scene.name == "PauseMenuScene" || scene.name == "SkillSelection")
        {
          continue;
        }

        // Check if this scene has EncounterManager or MenuManager (battle scene markers)
        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
          if (obj.GetComponent<EncounterManager>() != null ||
              obj.GetComponent<MenuManager>() != null ||
              obj.GetComponentInChildren<EncounterManager>() != null ||
              obj.GetComponentInChildren<MenuManager>() != null)
          {
            battleScene = scene;
            break;
          }
        }

        if (battleScene.IsValid())
        {
          break;
        }
      }

      // If we found the battle scene, reload it
      if (battleScene.IsValid())
      {
        SceneManager.LoadScene(battleScene.name);
      }
      else
      {
        // Fallback: try to find by build index (assuming battle scene is index 0)
        // Or reload the first non-menu scene
        Debug.LogWarning("PauseMenuController: Could not find battle scene, attempting fallback");

        // Try scene at index 0 (usually the main game scene)
        if (SceneManager.sceneCountInBuildSettings > 0)
        {
          SceneManager.LoadScene(0);
        }
        else
        {
          Debug.LogError("PauseMenuController: No scenes in build settings!");
        }
      }
    }

    /// <summary>
    /// Exits the game (called after confirmation)
    /// </summary>
    private void ExitGame()
    {
      Time.timeScale = 1f; // Always unpause before exiting
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }
  }
}