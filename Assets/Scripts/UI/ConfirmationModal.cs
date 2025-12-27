using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace TinySwords2D.UI
{
  /// <summary>
  /// A reusable confirmation modal dialog with Yes/No buttons.
  /// </summary>
  public class ConfirmationModal : MonoBehaviour
  {
    [Header("UI References")]
    [SerializeField] private GameObject modalPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton; // This is your "Cancel" button

    private System.Action onConfirm;
    private System.Action onCancel;

    private void Awake()
    {
      // Hide by default
      if (modalPanel != null)
      {
        modalPanel.SetActive(false);
      }

      // Setup button listeners
      if (yesButton != null)
      {
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(OnYesClicked);
      }

      if (noButton != null)
      {
        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(OnNoClicked); // This handles "Cancel"
      }
    }

    private void Update()
    {
      // ESC key closes the modal (same as clicking Cancel/No)
      if (IsOpen() && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
      {
        OnNoClicked(); // This will call onCancel (if set) and Hide()
      }
    }

    /// <summary>
    /// Shows the confirmation modal with a message and callbacks
    /// </summary>
    public void Show(string message, System.Action onConfirm, System.Action onCancel = null)
    {
      this.onConfirm = onConfirm;
      this.onCancel = onCancel;

      if (messageText != null)
      {
        messageText.text = message;
      }

      if (modalPanel != null)
      {
        modalPanel.SetActive(true);
      }
    }

    /// <summary>
    /// Hides the confirmation modal
    /// </summary>
    public void Hide()
    {
      if (modalPanel != null)
      {
        modalPanel.SetActive(false);
      }

      onConfirm = null;
      onCancel = null;
    }

    /// <summary>
    /// Checks if the modal is currently open
    /// </summary>
    public bool IsOpen()
    {
      return modalPanel != null && modalPanel.activeSelf;
    }

    /// <summary>
    /// Called when Yes/Confirm button is clicked
    /// </summary>
    private void OnYesClicked()
    {
      onConfirm?.Invoke(); // Execute the confirm action (RestartGame or ExitGame)
      Hide(); // Close the modal
    }

    /// <summary>
    /// Called when No/Cancel button is clicked or ESC is pressed
    /// </summary>
    private void OnNoClicked()
    {
      onCancel?.Invoke(); // Execute cancel action (if any, currently null)
      Hide(); // Close the modal
    }
  }
}