using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System; // Add this for Action event

namespace TinySwords2D.UI
{
  /// <summary>
  /// Manages loading/unloading menu scenes additively (pause menu, skill selection, etc.).
  /// Can be reused from any scene in the game.
  /// </summary>
  public class MenuManager : MonoBehaviour
  {
    public static MenuManager Instance { get; private set; }

    [Header("Menu Scene Settings")]
    [Tooltip("Name of the pause menu scene (must be in Build Settings)")]
    [SerializeField] private string pauseMenuSceneName = "PauseMenuScene";

    [Tooltip("Name of the skill selection scene (must be in Build Settings)")]
    [SerializeField] private string skillSelectionSceneName = "SkillSelection";

    [Header("Pause Settings")]
    [Tooltip("Should the game pause when menu is open?")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    private bool isMenuOpen = false;
    private Scene? menuScene;
    private string currentMenuType = ""; // Track which menu is open: "pause" or "skillSelection"

    // Event for when skill selection is completed
    public event Action OnSkillSelectionComplete;

    private void Awake()
    {
      // Singleton pattern
      if (Instance == null)
      {
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scene loads
      }
      else
      {
        Destroy(gameObject);
      }
    }

    private void Update()
    {
      // ESC key is handled by PauseMenuController when menu is open
      // Only handle ESC when menu is closed (to open it)
      if (!isMenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        OpenMenu();
      }
    }

    /// <summary>
    /// Toggles the pause menu on/off
    /// </summary>
    public void TogglePauseMenu()
    {
      if (isMenuOpen)
      {
        CloseMenu();
      }
      else
      {
        OpenMenu();
      }
    }

    /// <summary>
    /// Opens the pause menu scene additively
    /// </summary>
    public void OpenMenu()
    {
      if (isMenuOpen) return;

      StartCoroutine(LoadMenuSceneCoroutine(pauseMenuSceneName, "pause"));
    }

    /// <summary>
    /// Opens the skill selection scene additively (pauses game)
    /// </summary>
    public void OpenSkillSelection()
    {
      if (isMenuOpen) return;

      StartCoroutine(LoadMenuSceneCoroutine(skillSelectionSceneName, "skillSelection"));
    }

    private IEnumerator LoadMenuSceneCoroutine(string sceneName, string menuType)
    {
      // Check if scene exists in build settings
      if (!SceneExists(sceneName))
      {
        Debug.LogError($"MenuManager: Scene '{sceneName}' not found in Build Settings!");
        yield break;
      }

      // Load the menu scene additively
      AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

      // Wait until scene is fully loaded
      while (!asyncLoad.isDone)
      {
        yield return null;
      }

      // Find the menu scene
      menuScene = SceneManager.GetSceneByName(sceneName);

      if (menuScene.HasValue && menuScene.Value.IsValid())
      {
        // Set the menu scene as active (for UI rendering)
        SceneManager.SetActiveScene(menuScene.Value);

        currentMenuType = menuType;

        // Wait a frame for scene to fully initialize
        yield return null;

        // Ensure EventSystem is set up properly
        SetupEventSystem();

        // Setup controller based on menu type
        if (menuType == "pause")
        {
          // Find and setup the PauseMenuController in the loaded scene
          PauseMenuController menuController = FindFirstObjectByType<PauseMenuController>();
          if (menuController != null)
          {
            menuController.Initialize(this);
            
            // Select first button to ensure EventSystem is ready
            SelectFirstButton(menuController);
          }
        }
        else if (menuType == "skillSelection")
        {
          // Find and setup the SkillSelectionController in the loaded scene
          SkillSelectionController skillController = FindFirstObjectByType<SkillSelectionController>();
          if (skillController != null)
          {
            skillController.Initialize(this);
          }
        }

        isMenuOpen = true;

        // Pause the game
        if (pauseTimeWhenOpen)
        {
          Time.timeScale = 0f;
        }

        // Wait one more frame to ensure everything is ready
        yield return null;

        Debug.Log($"MenuManager: {menuType} menu opened from scene '{SceneManager.GetActiveScene().name}'");
      }
      else
      {
        Debug.LogError($"MenuManager: Failed to load menu scene '{sceneName}'");
      }
    }

    private void SetupEventSystem()
    {
      // Find or create EventSystem
      EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
      
      if (eventSystem == null)
      {
        // Create EventSystem if it doesn't exist
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystem = eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
      }

      // Ensure EventSystem is enabled
      if (eventSystem != null)
      {
        eventSystem.enabled = false;
        eventSystem.enabled = true;
      }
    }

    private void SelectFirstButton(PauseMenuController menuController)
    {
      // Try to select the resume button or first available button
      if (menuController != null)
      {
        // Use reflection or add a public method to get the resume button
        // For now, we'll just ensure EventSystem is ready
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
          // Clear current selection to reset EventSystem state
          eventSystem.SetSelectedGameObject(null);
          
          // Wait a frame then select first button
          StartCoroutine(SelectFirstButtonDelayed());
        }
      }
    }

    private IEnumerator SelectFirstButtonDelayed()
    {
      yield return null; // Wait one frame
      
      EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
      if (eventSystem != null)
      {
        // Find first selectable UI element
        Selectable firstSelectable = FindFirstObjectByType<Selectable>();
        if (firstSelectable != null)
        {
          eventSystem.SetSelectedGameObject(firstSelectable.gameObject);
        }
      }
    }

    /// <summary>
    /// Closes the pause menu scene
    /// </summary>
    public void CloseMenu()
    {
      if (!isMenuOpen) return;

      StartCoroutine(UnloadMenuSceneCoroutine());
    }

    private IEnumerator UnloadMenuSceneCoroutine()
    {
      // Unpause the game first
      Time.timeScale = 1f;

      if (menuScene.HasValue && menuScene.Value.IsValid())
      {
        // Unload the menu scene
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(menuScene.Value);

        while (!asyncUnload.isDone)
        {
          yield return null;
        }

        menuScene = null;
      }

      isMenuOpen = false;
      currentMenuType = "";
      Debug.Log("MenuManager: Menu closed");
    }

    /// <summary>
    /// Called by SkillSelectionController when resume is clicked
    /// </summary>
    public void OnSkillSelectionResume()
    {
      CloseMenu();
      OnSkillSelectionComplete?.Invoke();
    }

    /// <summary>
    /// Checks if a scene exists in the build settings
    /// </summary>
    private bool SceneExists(string sceneName)
    {
      for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
      {
        string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
        string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        if (sceneNameInBuild == sceneName)
        {
          return true;
        }
      }
      return false;
    }

    private void OnDestroy()
    {
      // Safety: ensure time scale is reset
      Time.timeScale = 1f;
    }
  }
}